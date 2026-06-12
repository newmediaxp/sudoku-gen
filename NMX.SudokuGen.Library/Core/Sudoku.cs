namespace NMX.SudokuGen.Library.Core;

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
    private readonly int[] altSolution, randomRows;
    private readonly uint[] rowMask, colMask, segMask;
    private readonly uint fullMask;
    private readonly int[] rowOf, colOf, segOf;
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
        altSolution = new int[squares];
        randomRows = new int[squares];
        rowMask = new uint[rows];
        colMask = new uint[rows];
        segMask = new uint[rows];
        fullMask = ((1u << rows) - 1) << 1;
        rowOf = new int[squares];
        colOf = new int[squares];
        segOf = new int[squares];
        for (int i = 0; i < squares; ++i)
        {
            rowOf[i] = i / rows;
            colOf[i] = i % rows;
            segOf[i] = i / squaresInSegmentRow * rank + i % rows / rank;
        }
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
        Sudoku a_sudoku = new(p_rank, p_remove, p_random);
        a_sudoku.FillAll();
        a_sudoku.Prune();
        // a final shuffle washes out the positional bias of the backtracking fill
        return a_sudoku.Shuffle(p_random);
    }

    #region Conflict

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
        if (Search_Row(p_board, a_input, p_idx)) a_conflicts |= Conflict.Row;
        if (Search_Column(p_board, a_input, p_idx)) a_conflicts |= Conflict.Column;
        if (Search_Segment(p_board, a_input, p_idx)) a_conflicts |= Conflict.Segment;
        return a_conflicts;
    }

    /// <summary>
    /// True when <paramref name="p_input"/> also occurs in <paramref name="p_idx"/>'s row (<paramref name="p_idx"/> itself excluded).
    /// Serves <see cref="FindConflicts"/> on caller-supplied boards; the solver itself relies on the candidate masks instead.
    /// </summary>
    private bool Search_Row(in int[] p_arr, in int p_input, in int p_idx)
    {
        for (int a_startIdx = rowOf[p_idx] * rows, a_limitIdx = a_startIdx + rows, i = a_startIdx; i < a_limitIdx; ++i)
            if (i != p_idx && p_arr[i] == p_input) return true;
        return false;
    }

    /// <summary>
    /// True when <paramref name="p_input"/> also occurs in <paramref name="p_idx"/>'s column (<paramref name="p_idx"/> itself excluded).
    /// Serves <see cref="FindConflicts"/> on caller-supplied boards; the solver itself relies on the candidate masks instead.
    /// </summary>
    private bool Search_Column(in int[] p_arr, in int p_input, in int p_idx)
    {
        for (int i = colOf[p_idx]; i < squares; i += rows)
            if (i != p_idx && p_arr[i] == p_input) return true;
        return false;
    }

    /// <summary>
    /// True when <paramref name="p_input"/> also occurs in <paramref name="p_idx"/>'s segment (<paramref name="p_idx"/> itself excluded).
    /// Serves <see cref="FindConflicts"/> on caller-supplied boards; the solver itself relies on the candidate masks instead.</summary>
    private bool Search_Segment(in int[] p_arr, in int p_input, in int p_idx)
    {
        int a_startIdx = rowOf[p_idx] / rank * squaresInSegmentRow + colOf[p_idx] / rank * rank;
        for (int i_row = 0; i_row < rank; ++i_row)
            for (int i_col = 0; i_col < rank; ++i_col)
            {
                int a_idx = a_startIdx + i_row * rows + i_col;
                if (a_idx != p_idx && p_arr[a_idx] == p_input) return true;
            }
        return false;
    }

    #endregion

    #region Shuffle

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
        Sudoku a_sudoku = new(rank, remove, p_random) { Removed = Removed };
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

    #endregion

    /// <summary>
    /// Seeds the diagonal segments with random permutations. They never share a row or column, so they can be filled independently without backtracking.
    /// </summary>
    private void Set_DiagonalSegments(in int[] p_arr)
    {
        int[] a_inputs = new int[squaresInSegmentRow];
        InitRandom(random, a_inputs, rows, true);
        for (int i_seg = 0; i_seg < rank; ++i_seg)
            for (int a_startIdx = i_seg * rank * (rows + 1), i_row = 0; i_row < rank; ++i_row)
                for (int i_col = 0; i_col < rank; ++i_col)
                {
                    int a_idx = a_startIdx + i_row * rows + i_col;
                    p_arr[a_idx] = a_inputs[i_seg * rows + i_row * rank + i_col];
                }
    }



    #region Fill

    /// <summary>
    /// Candidate order used by the fill backtrackers (<see cref="FillConstrained"/>, <see cref="FillSequential"/>) for each blank square.
    /// </summary>
    private enum FillMode : byte
    {
        /// <summary>
        /// 1..<see cref="rows"/> in order — deterministic solving.
        /// </summary>
        NoInput,
        /// <summary>
        /// The pre-shuffled values of <see cref="randomRows"/>'s row — randomised generation.
        /// </summary>
        RandomRow,
        /// <summary>
        /// The <see cref="solution"/> value + 1 onwards, wrapping, so the known solution value is tried last.
        /// Completing the grid before reaching it proves a second solution exists (see <see cref="Unique"/>).
        /// </summary>
        Uniqueness
    }

    /// <summary>
    /// Rebuilds <see cref="rowMask"/>, <see cref="colMask"/> and <see cref="segMask"/> from
    /// <paramref name="p_arr"/>: bit v of a unit's mask is set when value v is placed in that unit.
    /// </summary>
    /// <param name="p_arr">The grid the masks should describe.</param>
    private void LoadMasks(in int[] p_arr)
    {
        for (int i = 0; i < rows; ++i) { rowMask[i] = 0; colMask[i] = 0; segMask[i] = 0; }
        uint a_valueBit;
        for (int i = 0; i < squares; ++i)
        {
            if (p_arr[i] == 0) continue;
            a_valueBit = 1u << p_arr[i];
            rowMask[rowOf[i]] |= a_valueBit; colMask[colOf[i]] |= a_valueBit; segMask[segOf[i]] |= a_valueBit;
        }
    }

    /// <summary>
    /// Recursive backtracker over the blank squares of <paramref name="p_arr"/> from <paramref name="p_idx"/> onwards, in grid order.
    /// <paramref name="p_mode"/> decides the candidate order per square.
    /// The masks must describe <paramref name="p_arr"/> on entry (see <see cref="LoadMasks"/>).
    /// They are kept in sync while filling, but are stale after a completed fill.
    /// Kept as the straightforward reference implementation; the solver runs <see cref="FillConstrained"/> instead.
    /// </summary>
    /// <param name="p_arr">The grid to fill, <see cref="solution"/> or <see cref="altSolution"/>.</param>
    /// <param name="p_mode">Candidate order per square.</param>
    /// <param name="p_idx">First square to consider; filled squares are skipped.</param>
    /// <returns>True when the grid was completed; a failed fill leaves <paramref name="p_arr"/> and the masks unchanged.</returns>
    private bool FillSequential(in int[] p_arr, in FillMode p_mode, int p_idx)
    {
        if (p_idx >= squares) return true;
        while (p_arr[p_idx] != 0) { ++p_idx; if (p_idx >= squares) return true; }
        int a_row = rowOf[p_idx], a_col = colOf[p_idx], a_seg = segOf[p_idx];
        for (int a_input, i = 0; i < rows; ++i)
        {
            if (p_mode == FillMode.NoInput) a_input = i + 1;
            else if (p_mode == FillMode.RandomRow) a_input = randomRows[a_row * rows + i];
            else if (p_mode == FillMode.Uniqueness) { a_input = solution[p_idx] + i + 1; if (a_input > rows) a_input -= rows; }
            else a_input = -1;
            uint a_inputBit = 1u << a_input;
            if (((rowMask[a_row] | colMask[a_col] | segMask[a_seg]) & a_inputBit) != 0) continue;
            p_arr[p_idx] = a_input;
            rowMask[a_row] |= a_inputBit; colMask[a_col] |= a_inputBit; segMask[a_seg] |= a_inputBit;
            if (FillSequential(p_arr, p_mode, p_idx + 1)) return true;
            rowMask[a_row] &= ~a_inputBit; colMask[a_col] &= ~a_inputBit; segMask[a_seg] &= ~a_inputBit;
            p_arr[p_idx] = 0;
        }
        return false;
    }

    /// <summary>
    /// Recursive backtracker over the blank squares of <paramref name="p_arr"/>, always working on the
    /// most constrained blank (fewest legal candidates) first. Forced squares are placed without branching
    /// and dead grids are detected the moment any blank has no candidate left, which keeps the search tree
    /// small where grid-order filling degenerates (high ranks, many blanks).
    /// <paramref name="p_mode"/> decides the candidate order per square.
    /// The masks must describe <paramref name="p_arr"/> on entry (see <see cref="LoadMasks"/>).
    /// They are kept in sync while filling, but are stale after a completed fill — enter via <see cref="FillRemaining"/>.
    /// </summary>
    /// <param name="p_arr">The grid to fill, <see cref="solution"/> or <see cref="altSolution"/>.</param>
    /// <param name="p_mode">Candidate order per square.</param>
    /// <returns>True when the grid was completed; a failed fill leaves <paramref name="p_arr"/> and the masks unchanged.</returns>
    private bool FillConstrained(in int[] p_arr, in FillMode p_mode)
    {
        int a_bestIdx = -1, a_bestCount = int.MaxValue;
        uint a_bestFreeMask = 0;
        for (int i = 0; i < squares; ++i)
        {
            if (p_arr[i] != 0) continue;
            uint a_freeMask = ~(rowMask[rowOf[i]] | colMask[colOf[i]] | segMask[segOf[i]]) & fullMask;
            int a_count = PopCount(a_freeMask);
            if (a_count == 0) return false;
            if (a_count >= a_bestCount) continue;
            a_bestCount = a_count; a_bestIdx = i; a_bestFreeMask = a_freeMask;
            // a forced square cannot be beaten, except by a dead one ending the fill anyway
            if (a_bestCount == 1) break;
        }
        if (a_bestIdx == -1) return true;
        int a_row = rowOf[a_bestIdx], a_col = colOf[a_bestIdx], a_seg = segOf[a_bestIdx];
        for (int a_input, i = 0; i < rows; ++i)
        {
            if (p_mode == FillMode.NoInput) a_input = i + 1;
            else if (p_mode == FillMode.RandomRow) a_input = randomRows[a_row * rows + i];
            else if (p_mode == FillMode.Uniqueness) { a_input = solution[a_bestIdx] + i + 1; if (a_input > rows) a_input -= rows; }
            else a_input = -1;
            uint a_inputBit = 1u << a_input;
            if ((a_bestFreeMask & a_inputBit) == 0) continue;
            p_arr[a_bestIdx] = a_input;
            rowMask[a_row] |= a_inputBit; colMask[a_col] |= a_inputBit; segMask[a_seg] |= a_inputBit;
            if (FillConstrained(p_arr, p_mode)) return true;
            rowMask[a_row] &= ~a_inputBit; colMask[a_col] &= ~a_inputBit; segMask[a_seg] &= ~a_inputBit;
            p_arr[a_bestIdx] = 0;
        }
        return false;
    }

    /// <summary>
    /// Syncs the masks to <paramref name="p_arr"/> via <see cref="LoadMasks"/>, then runs <see cref="FillConstrained"/> over the whole grid.
    /// </summary>
    /// <param name="p_arr">The grid to fill, <see cref="solution"/> or <see cref="altSolution"/>.</param>
    /// <param name="p_mode">Candidate order per square.</param>
    /// <returns>True when the grid was completed; a failed fill leaves <paramref name="p_arr"/> unchanged.</returns>
    private bool FillRemaining(in int[] p_arr, in FillMode p_mode)
    {
        LoadMasks(p_arr);
        return FillConstrained(p_arr, p_mode);
    }

    /// <summary>
    /// Generates a complete random <see cref="solution"/> grid: seeds the diagonal segments, then backtrack-fills the rest in randomised order.
    /// </summary>
    /// <exception cref="InvalidOperationException">The grid could not be completed. Impossible for an empty grid, so this signals a bug in the fill logic itself.</exception>
    private void FillAll()
    {
        if (rank >= 3) Set_DiagonalSegments(solution);
        InitRandom(random, randomRows, rows, true);
        _ = FillRemaining(solution, FillMode.RandomRow);
        if (Count(solution, 0) > 0) throw new InvalidOperationException("cannot create, logic error");
    }

    /// <summary>
    /// Completes <see cref="solution"/> from the already-validated puzzle inputs by deterministic backtracking.
    /// </summary>
    /// <exception cref="InvalidOperationException">The puzzle has no solution, e.g. inputs that are conflict-free but still unsatisfiable.</exception>
    private void FillRest()
    {
        _ = FillRemaining(solution, FillMode.NoInput);
        if (Count(solution, 0) > 0) throw new InvalidOperationException("cannot solve, logic error");
    }

    #endregion

    /// <summary>
    /// True when <see cref="puzzle"/> stays uniquely solvable after blanking <paramref name="p_idx"/>:
    /// the puzzle was unique before the removal, so a second solution would have to place a
    /// different value at that square — only the other conflict-free candidates there are probed for solvability.
    /// </summary>
    /// <param name="p_idx">Index of the square just blanked in <see cref="puzzle"/>.</param>
    /// <param name="p_removed">The <see cref="solution"/> value the square held before blanking.</param>
    /// <returns>True when no alternative value at <paramref name="p_idx"/> completes the puzzle.</returns>
    private bool UniqueWithout(in int p_idx, in int p_removed)
    {
        Copy(puzzle, altSolution);
        LoadMasks(altSolution);
        // captured before the probes below rebuild the masks
        uint a_usedMask = rowMask[rowOf[p_idx]] | colMask[colOf[p_idx]] | segMask[segOf[p_idx]];
        for (int a_input = 1; a_input <= rows; ++a_input)
        {
            if (a_input == p_removed || (a_usedMask & 1u << a_input) != 0) continue;
            altSolution[p_idx] = a_input;
            if (FillRemaining(altSolution, FillMode.NoInput)) return false;
            // a failed fill backtracks every square it set, so only p_idx needs resetting
            altSolution[p_idx] = 0;
        }
        return true;
    }

    /// <summary>
    /// Blanks random squares one at a time, keeping only removals that preserve uniqueness (via <see cref="UniqueWithout"/>), until <see cref="remove"/> blanks are reached or no square can be removed.
    /// </summary>
    private void Prune()
    {
        int[] a_indexes = new int[squares];
        InitRandom(random, a_indexes, squares, false);
        Copy(solution, puzzle); Removed = 0;
        for (int a_input, i = 0; i < a_indexes.Length && Removed < remove; ++i)
        {
            a_input = puzzle[a_indexes[i]]; if (a_input == 0) continue;
            puzzle[a_indexes[i]] = 0; if (UniqueWithout(a_indexes[i], a_input)) { ++Removed; continue; }
            puzzle[a_indexes[i]] = a_input;
        }
    }

    #region Solve

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
        Sudoku a_sudoku = new(a_rank, Count(p_puzz, 0), new Random());
        a_sudoku.Removed = a_sudoku.remove;
        for (int i = 0; i < p_puzz.Count; ++i) a_sudoku.puzzle[i] = p_puzz[i];
        if (!a_sudoku.Valid(a_sudoku.puzzle)) throw new InvalidOperationException("invalid puzzle, duplicate inputs found");
        Copy(a_sudoku.puzzle, a_sudoku.solution);
        a_sudoku.FillRest();
        if (!a_sudoku.Unique()) throw new InvalidOperationException("invalid puzzle, no unique solution");
        return a_sudoku;
    }

    /// <summary>
    /// True when every non-blank square is within 1..<see cref="rows"/> and conflict-free.
    /// Repurposes the candidate masks as scratch; every fill resyncs them anyway (see <see cref="FillRemaining"/>).
    /// </summary>
    private bool Valid(in int[] p_arr)
    {
        for (int i = 0; i < rows; ++i) { rowMask[i] = 0; colMask[i] = 0; segMask[i] = 0; }
        for (int i = 0; i < p_arr.Length; ++i)
        {
            if (p_arr[i] == 0) continue;
            if (p_arr[i] < 0 || p_arr[i] > rows) return false;
            uint a_valueBit = 1u << p_arr[i];
            if (((rowMask[rowOf[i]] | colMask[colOf[i]] | segMask[segOf[i]]) & a_valueBit) != 0) return false;
            rowMask[rowOf[i]] |= a_valueBit; colMask[colOf[i]] |= a_valueBit; segMask[segOf[i]] |= a_valueBit;
        }
        return true;
    }

    /// <summary>
    /// True when <see cref="solution"/> is the only completion of <see cref="puzzle"/>: the Uniqueness fill steers away from the known solution, so ending up identical proves uniqueness.
    /// </summary>
    private bool Unique()
    {
        Copy(puzzle, altSolution);
        _ = FillRemaining(altSolution, FillMode.Uniqueness); return Same(altSolution, solution);
    }

    #endregion

}
