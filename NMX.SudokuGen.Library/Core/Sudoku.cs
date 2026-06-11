namespace NMX.SudokuGen.Library.Core
{
    using System;
    using System.Collections.Generic;
    using static Utility;

    /// <summary>
    /// An immutable sudoku puzzle/solution pair, obtained via
    /// <see cref="Create(in int, in int)"/> (new random puzzle),
    /// <see cref="Solve"/> (from existing inputs) or
    /// <see cref="Shuffle()"/> (equivalent variant).
    /// Grids are flat row-by-row lists of <see cref="squares"/> values, 0 = blank.
    /// No shared mutable state: instances are independent and parallel use is safe.
    /// </summary>
    public sealed class Sudoku
    {
        /// <summary>Lowest supported <see cref="rank"/>.</summary>
        public const int minRank = 2;
        /// <summary>Highest supported <see cref="rank"/>; bounded by the single-character
        /// puzzle code format (values above 35 are unrepresentable) and by generation cost,
        /// which grows steeply with rank.</summary>
        public const int maxRank = 5;

        /// <summary>
        /// Segment size of the sudoku: 2 = 4x4 up to 5 = 25x25.
        /// </summary>
        public readonly int rank;

        /// <summary>
        /// Requested number of blank squares; 
        /// <see cref="Removed"/> holds the achieved count.
        /// </summary>
        public readonly int remove;

        /// <summary>
        /// Squares per row, column and segment. <see cref="rank"/> squared.
        /// </summary>
        public readonly int rows;

        /// <summary>
        /// Total squares in the grid. <see cref="rows"/> squared.
        /// </summary>
        public readonly int squares;

        private readonly int squaresInSegmentRow;
        private readonly int[] puzzle, solution;
        private readonly int[] altSol;
        private readonly Random random;

        /// <summary>
        /// Actual number of blanks in the puzzle. Can be lower than <see cref="remove"/> when further removal would break uniqueness.
        /// </summary>
        public int Removed { get; private set; }

        /// <summary>
        /// The puzzle grid, 0 = blank.
        /// </summary>
        public IReadOnlyList<int> Puzzle => puzzle;

        /// <summary>
        /// The unique completed solution of <see cref="Puzzle"/>.
        /// </summary>
        public IReadOnlyList<int> Solution => solution;

        /// <exception cref="ArgumentException">The rank is outside <see cref="minRank"/>..<see cref="maxRank"/>,
        /// or the requested blanks are negative.</exception>
        private Sudoku(in int p_rank, in int p_remove, in Random p_random)
        {
            if (p_rank < minRank || p_rank > maxRank)
                throw new ArgumentException($"rank must be within {minRank}..{maxRank}", nameof(p_rank));
            if (p_remove < 0)
                throw new ArgumentException("requested blanks cannot be negative", nameof(p_remove));
            rank = p_rank;
            remove = p_remove;
            random = p_random;
            rows = rank * rank;
            squares = rows * rows;
            squaresInSegmentRow = rows * rank;
            puzzle = new int[squares];
            solution = new int[squares];
            altSol = new int[squares];
        }

        /// <summary>
        /// Creates a random sudoku while keeping the solution unique (<see cref="Removed"/> holds the achieved blank count).
        /// </summary>
        /// <param name="p_rank">Segment size of the sudoku, <see cref="minRank"/> to <see cref="maxRank"/>.</param>
        /// <param name="p_remove">Desired number of blank squares. Fewer are blanked when further removal would break uniqueness.</param>
        /// <exception cref="ArgumentException">The rank is outside <see cref="minRank"/>..<see cref="maxRank"/>,
        /// or the requested blanks are negative.</exception>
        public static Sudoku Create(in int p_rank, in int p_remove) => Create(p_rank, p_remove, new Random());

        /// <summary>Seeded variant of Create: the same seed always reproduces the same sudoku.</summary>
        /// <param name="p_rank">Segment size of the sudoku, <see cref="minRank"/> to <see cref="maxRank"/>.</param>
        /// <param name="p_remove">Desired number of blank squares. Fewer are blanked when further removal would break uniqueness.</param>
        /// <param name="p_seed">Seed for the random generator. Determines the resulting sudoku.</param>
        /// <exception cref="ArgumentException">The rank is outside <see cref="minRank"/>..<see cref="maxRank"/>,
        /// or the requested blanks are negative.</exception>
        public static Sudoku Create(in int p_rank, in int p_remove, in int p_seed) => Create(p_rank, p_remove, new Random(p_seed));

        private static Sudoku Create(in int p_rank, in int p_remove, in Random p_random)
        {
            Sudoku a_sudoku = new Sudoku(p_rank, p_remove, p_random);
            a_sudoku.FillAll();
            a_sudoku.Prune();
            // a final shuffle washes out the positional bias of the backtracking fill
            return a_sudoku.Shuffle(p_random);
        }

        /// <summary>
        /// Solves the given puzzle. Rank is inferred from its length.
        /// </summary>
        /// <param name="p_puzz">The puzzle grid, row by row, 0 = blank. Length must be a 4th power (16, 81, ...).</param>
        /// <returns>A sudoku whose <see cref="Solution"/> is the unique completion of the input.</returns>
        /// <exception cref="InvalidOperationException">The length is not the 4th power of a rank
        /// within <see cref="minRank"/>..<see cref="maxRank"/>, the inputs conflict,
        /// or the puzzle does not have exactly one solution.</exception>
        public static Sudoku Solve(in IReadOnlyList<int> p_puzz)
        {
            int a_rank = (int)Math.Sqrt(Math.Sqrt(p_puzz.Count));
            if (a_rank * a_rank * a_rank * a_rank != p_puzz.Count)
                throw new InvalidOperationException("invalid puzzle length, could not determine rank");
            if (a_rank < minRank || a_rank > maxRank)
                throw new InvalidOperationException($"invalid puzzle length, rank must be within {minRank}..{maxRank}");
            Sudoku a_sudoku = new Sudoku(a_rank, Count(p_puzz, 0), new Random());
            a_sudoku.Removed = a_sudoku.remove;
            for (int i = 0; i < p_puzz.Count; ++i) a_sudoku.puzzle[i] = p_puzz[i];
            if (!a_sudoku.Valid(a_sudoku.puzzle)) throw new InvalidOperationException("invalid puzzle, duplicate inputs found");
            Copy(a_sudoku.puzzle, a_sudoku.solution);
            a_sudoku.FillRest();
            if (!a_sudoku.Unique()) throw new InvalidOperationException("invalid puzzle, no unique solution");
            return a_sudoku;
        }

        /// <summary>
        /// True when <paramref name="p_input"/> also occurs in <paramref name="p_idx"/>'s row or column (<paramref name="p_idx"/> itself excluded).
        /// </summary>
        private bool Search_RowCol(in int[] p_arr, in int p_input, in int p_idx)
        {
            for (int a_startRow = p_idx / rows * rows, a_endRow = a_startRow + rows,
                i = a_startRow; i < a_endRow && i < squares; ++i) if (i != p_idx && p_arr[i] == p_input) return true;
            for (int a_startCol = p_idx % rows,
                i = a_startCol; i < squares; i += rows) if (i != p_idx && p_arr[i] == p_input) return true;
            return false;
        }

        /// <summary>
        /// True when <paramref name="p_input"/> also occurs in <paramref name="p_idx"/>'s segment (<paramref name="p_idx"/> itself excluded).</summary>
        private bool Search_Segment(in int[] p_arr, in int p_input, in int p_idx)
        {
            for (int a_startX = p_idx / rank * rank, a_startO = a_startX - a_startX / rows % rank * rows,
                a_o, i = 0; i < rows; ++i)
            {
                a_o = a_startO + i / rank * rows + i % rank;
                if (a_o != p_idx && p_arr[a_o] == p_input) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a new equivalent sudoku derived from this one by symmetry transformations:
        /// permuted segment rows/columns, permuted rows/columns within segments,
        /// relabelled digits and an optional transpose. The result is guaranteed to stay
        /// valid and uniquely solvable, with the same rank, difficulty and blank count.
        /// </summary>
        public Sudoku Shuffle() => Shuffle(new Random());

        /// <summary>
        /// Seeded variant of <see cref="Shuffle()"/>: the same seed always reproduces the same variant.
        /// </summary>
        /// <param name="p_seed">Seed for the random generator; determines the resulting variant.</param>
        public Sudoku Shuffle(in int p_seed) => Shuffle(new Random(p_seed));

        private Sudoku Shuffle(in Random p_random)
        {
            Sudoku a_sudoku = new Sudoku(rank, remove, p_random) { Removed = Removed };
            int[] a_rowMap = AxisMap(p_random), a_colMap = AxisMap(p_random);
            int[] a_digitMap = new int[rows + 1], a_digits = new int[rows];
            InitRandom(p_random, a_digits, rows, true);
            for (int i = 0; i < rows; ++i) a_digitMap[i + 1] = a_digits[i];
            bool a_transpose = p_random.Next(2) == 1;
            for (int a_from, a_to, i_row = 0; i_row < rows; ++i_row)
                for (int i_col = 0; i_col < rows; ++i_col)
                {
                    a_from = a_rowMap[i_row] * rows + a_colMap[i_col];
                    a_to = a_transpose ? i_col * rows + i_row : i_row * rows + i_col;
                    a_sudoku.puzzle[a_to] = a_digitMap[puzzle[a_from]];
                    a_sudoku.solution[a_to] = a_digitMap[solution[a_from]];
                }
            return a_sudoku;
        }

        /// <summary>
        /// Builds an index map for one axis: permutes segments and lines within each segment, so lines never leave their (relocated) segment and validity is preserved.
        /// </summary>
        private int[] AxisMap(in Random p_random)
        {
            int[] a_map = new int[rows], a_segs = new int[rank], a_lines = new int[rank];
            InitRandom(p_random, a_segs, rank, false);
            for (int i_seg = 0; i_seg < rank; ++i_seg)
            {
                InitRandom(p_random, a_lines, rank, false);
                for (int i = 0; i < rank; ++i) a_map[i_seg * rank + i] = a_segs[i_seg] * rank + a_lines[i];
            }
            return a_map;
        }

        [Flags]
        public enum Conflict : byte
        {
            None = 0,
            Row = 1,
            Column = 2,
            Segment = 4
        }

        /// <summary>
        /// Checks the value already placed at <paramref name="p_idx"/> against the rest of <paramref name="p_board"/> and reports every unit where it appears again. 
        /// A blank square (0) never conflicts. 
        /// </summary>
        /// <param name="p_board">The board being played, row by row, 0 = blank;
        /// must have exactly <see cref="squares"/> squares.</param>
        /// <param name="p_idx">Index of the square to check.</param>
        /// <returns>Flags for each conflicting unit (row, column, segment), or <see cref="Conflict.None"/>.</returns>
        /// <exception cref="ArgumentException">The board is null or not exactly
        /// <see cref="squares"/> long, the index is outside the board, or the value
        /// at the index is outside 1..<see cref="rows"/>.</exception>
        public Conflict FindConflicts(in int[] p_board, in int p_idx)
        {
            if (p_board == null || p_board.Length != squares)
                throw new ArgumentException($"board must have exactly {squares} squares", nameof(p_board));
            if (p_idx < 0 || p_idx >= squares)
                throw new ArgumentException($"index must be within 0..{squares - 1}", nameof(p_idx));
            int a_input = p_board[p_idx];
            if (a_input == 0) return Conflict.None;
            if (a_input < 0 || a_input > rows)
                throw new ArgumentException($"value {a_input} at index {p_idx} is outside 1..{rows}", nameof(p_board));
            Conflict a_conflicts = Conflict.None;
            for (int a_start = p_idx / rows * rows, i = a_start; i < a_start + rows; ++i)
                if (i != p_idx && p_board[i] == a_input) { a_conflicts |= Conflict.Row; break; }
            for (int i = p_idx % rows; i < squares; i += rows)
                if (i != p_idx && p_board[i] == a_input) { a_conflicts |= Conflict.Column; break; }
            if (Search_Segment(p_board, a_input, p_idx)) a_conflicts |= Conflict.Segment;
            return a_conflicts;
        }

        /// <summary>
        /// True when every non-blank square is within 1..<see cref="rows"/> and conflict-free.
        /// </summary>
        private bool Valid(in int[] p_arr)
        {
            for (int i = 0; i < p_arr.Length; ++i)
            {
                if (p_arr[i] == 0) continue;
                if (p_arr[i] < 0 || p_arr[i] > rows
                    || Search_RowCol(p_arr, p_arr[i], i) || Search_Segment(p_arr, p_arr[i], i)) return false;
            }
            return true;
        }

        /// <summary>
        /// Seeds the diagonal segments with random permutations. They never share a row or column, so they can be filled independently without backtracking.
        /// </summary>
        private void Set_DiagonalSegments(in int[] p_arr)
        {
            int[] a_inputs = new int[squaresInSegmentRow];
            InitRandom(random, a_inputs, rows, true);
            for (int i = 0; i < squaresInSegmentRow; ++i)
                p_arr[i / rows * rank + i / rank * rows + i % rank] = a_inputs[i];
        }

        /// <summary>Candidate order used by <see cref="FillSequential"/> for each blank square.</summary>
        private enum FillMode : byte
        {
            /// <summary>
            /// 1..<see cref="rows"/> in order — deterministic solving.
            /// </summary>
            NoInput,
            /// <summary>
            /// The pre-shuffled values of <see cref="altSol"/>'s row — randomised generation.
            /// </summary>
            RandomRow,
            /// <summary>
            /// The <see cref="solution"/> value + 1 onwards, wrapping, so the known solution value is tried last.
            /// Completing the grid before reaching it proves a second solution exists (see <see cref="Unique"/>).
            /// </summary>
            Uniqueness
        }

        /// <summary>
        /// Recursive backtracker over the blank squares from <paramref name="p_idx"/> onwards.
        /// <paramref name="p_mode"/> decides the candidate order per square.
        /// </summary>
        private bool FillSequential(in FillMode p_mode, int p_idx)
        {
            if (p_idx >= squares) return true;
            int[] a_arr = p_mode == FillMode.Uniqueness ? altSol : solution;
            while (a_arr[p_idx] != 0) { ++p_idx; if (p_idx >= squares) return true; }
            for (int a_input, a_startRow = p_idx / rows * rows, i = 0; i < rows; ++i)
            {
                if (p_mode == FillMode.NoInput) a_input = i + 1;
                else if (p_mode == FillMode.RandomRow) a_input = altSol[a_startRow + i];
                else if (p_mode == FillMode.Uniqueness) { a_input = solution[p_idx] + i + 1; if (a_input > rows) a_input -= rows; }
                else a_input = -1;
                if (Search_RowCol(a_arr, a_input, p_idx) || Search_Segment(a_arr, a_input, p_idx)) continue;
                a_arr[p_idx] = a_input; if (FillSequential(p_mode, p_idx + 1)) return true;
                a_arr[p_idx] = 0;
            }
            return false;
        }

        private void FillRemaining(in FillMode p_mode) => FillSequential(p_mode, 0);

        /// <summary>
        /// Generates a complete random <see cref="solution"/> grid: seeds the diagonal segments, then backtrack-fills the rest in randomised order.
        /// </summary>
        /// <exception cref="InvalidOperationException">The grid could not be completed. Impossible for an empty grid, so this signals a bug in the fill logic itself.</exception>
        private void FillAll()
        {
            if (rank >= 3) Set_DiagonalSegments(solution);
            InitRandom(random, altSol, rows, true);
            FillRemaining(FillMode.RandomRow);
            if (Count(solution, 0) > 0) throw new InvalidOperationException("cannot create, logic error");
        }

        /// <summary>
        /// Completes <see cref="solution"/> from the already-validated puzzle inputs by deterministic backtracking.
        /// </summary>
        /// <exception cref="InvalidOperationException">The puzzle has no solution, e.g. inputs that are conflict-free but still unsatisfiable.</exception>
        private void FillRest()
        {
            FillRemaining(FillMode.NoInput);
            if (Count(solution, 0) > 0) throw new InvalidOperationException("cannot solve, logic error");
        }

        /// <summary>
        /// True when <see cref="solution"/> is the only completion of <see cref="puzzle"/>: the Uniqueness fill steers away from the known solution, so ending up identical proves uniqueness.
        /// </summary>
        private bool Unique()
        {
            Copy(puzzle, altSol);
            FillRemaining(FillMode.Uniqueness); return Same(altSol, solution);
        }

        /// <summary>
        /// Blanks random squares one at a time, keeping only removals that preserve uniqueness, until <see cref="remove"/> blanks are reached or no square can be removed.
        /// </summary>
        private void Prune()
        {
            int[] a_indexes = new int[squares];
            InitRandom(random, a_indexes, squares, false);
            Copy(solution, puzzle); Removed = 0;
            for (int a_input, i = 0; i < a_indexes.Length && Removed < remove; ++i)
            {
                a_input = puzzle[a_indexes[i]]; if (a_input == 0) continue;
                puzzle[a_indexes[i]] = 0; if (Unique()) { ++Removed; continue; }
                puzzle[a_indexes[i]] = a_input;
            }
        }
    }
}
