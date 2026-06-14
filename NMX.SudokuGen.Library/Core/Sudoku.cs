// Copyright (c) 2024-2026 New Media XP. Licensed under the MIT License.

namespace NMX.SudokuGen.Library.Core;

using System;
using System.Collections.Generic;
using static Utility;

/// <summary>
/// An immutable sudoku puzzle/solution pair, obtained via
/// <see cref="Create(in int, in int, in bool)"/> (new random puzzle),
/// <see cref="Solve"/> (from existing inputs) or
/// <see cref="Shuffle()"/> (equivalent variant).
/// Grids are flat row-by-row lists of <see cref="squares"/> values, 0 = blank.
/// No shared mutable state: instances are independent and parallel use is safe.
/// </summary>
public sealed class Sudoku
{
    public const int minRank = 2, maxRank = 5, maxBlanks = -1;

    /// <summary>
    /// Search nodes a single solvability trial in <see cref="UniqueWithout"/> may spend before giving up on the removal.
    /// Bounds <see cref="Create(in int, in int, in bool)"/>'s runtime at extreme blank counts without ever
    /// breaking uniqueness; bypassed by its exhaustive flag.
    /// Recalibrated for locked candidates (2026-06-12): max-blank counts stayed flat from 10 up to 100
    /// at ranks 4 and 5 while runtime scaled with the budget, so the smallest equal-quality value wins.
    /// </summary>
    private const int trialBudgetPerSquare = 10;

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
    private readonly uint[] rowOnceMask, colOnceMask, segOnceMask;
    private readonly uint[] rowTwiceMask, colTwiceMask, segTwiceMask;
    private readonly uint[] rowThriceMask, colThriceMask, segThriceMask;
    private readonly uint[] candMask;
    private readonly uint[] rowBlockMask, colBlockMask, rowBlockElim, colBlockElim;
    private readonly uint[] rowConfMask, colConfMask;
    private readonly uint[] nakedMasks;
    private readonly uint fullMask;
    private readonly int[] rowOf, colOf, segOf;
    private readonly int[] rowBlockOf, colBlockOf;
    private readonly int[] unitSquares, nakedSquares;
    private readonly Random random;
    private readonly int trialBudget;
    private int fillNodesLeft;

    /// <summary>
    /// Actual number of blanks in the puzzle. Can be lower than <see cref="remove"/> when further removal
    /// would break uniqueness (or exceed the internal search budget at extreme blank counts).
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
    private Sudoku(in int p_rank, in int p_remove, in Random p_random, in bool p_exhaustive = false)
    {
        if (p_rank < minRank || p_rank > maxRank)
            throw new ArgumentException($"rank must be within {minRank}..{maxRank}", nameof(p_rank));
        rank = p_rank;
        rows = rank * rank;
        squares = rows * rows;
        squaresInSegmentRow = rows * rank;
        if (p_remove < maxBlanks || p_remove > squares)
            throw new ArgumentException($"blanks must be within 0 .. rank^4", nameof(p_remove));
        remove = p_remove == maxBlanks ? squares : p_remove;
        random = p_random;
        trialBudget = p_exhaustive ? int.MaxValue : squares * trialBudgetPerSquare;
        puzzle = new int[squares];
        solution = new int[squares];
        altSolution = new int[squares];
        randomRows = new int[squares];
        rowMask = new uint[rows];
        colMask = new uint[rows];
        segMask = new uint[rows];
        rowOnceMask = new uint[rows];
        colOnceMask = new uint[rows];
        segOnceMask = new uint[rows];
        rowTwiceMask = new uint[rows];
        colTwiceMask = new uint[rows];
        segTwiceMask = new uint[rows];
        rowThriceMask = new uint[rows];
        colThriceMask = new uint[rows];
        segThriceMask = new uint[rows];
        candMask = new uint[squares];
        rowBlockMask = new uint[squaresInSegmentRow];
        colBlockMask = new uint[squaresInSegmentRow];
        rowBlockElim = new uint[squaresInSegmentRow];
        colBlockElim = new uint[squaresInSegmentRow];
        rowConfMask = new uint[rows];
        colConfMask = new uint[rows];
        fullMask = ((1u << rows) - 1) << 1;
        rowOf = new int[squares];
        colOf = new int[squares];
        segOf = new int[squares];
        rowBlockOf = new int[squares];
        colBlockOf = new int[squares];
        unitSquares = new int[3 * rows * rows];
        nakedSquares = new int[rows];
        nakedMasks = new uint[rows];
        int[] a_unitFill = new int[3 * rows];
        for (int a_unit, i = 0; i < squares; ++i)
        {
            rowOf[i] = i / rows;
            colOf[i] = i % rows;
            segOf[i] = i / squaresInSegmentRow * rank + i % rows / rank;
            rowBlockOf[i] = rowOf[i] * rank + colOf[i] / rank;
            colBlockOf[i] = colOf[i] * rank + rowOf[i] / rank;
            a_unit = rowOf[i]; unitSquares[a_unit * rows + a_unitFill[a_unit]++] = i;
            a_unit = rows + colOf[i]; unitSquares[a_unit * rows + a_unitFill[a_unit]++] = i;
            a_unit = 2 * rows + segOf[i]; unitSquares[a_unit * rows + a_unitFill[a_unit]++] = i;
        }
    }

    #region Create

    /// <summary>
    /// Creates a random sudoku while keeping the solution unique (<see cref="Removed"/> holds the achieved blank count).
    /// </summary>
    /// <param name="p_rank">Segment size of the sudoku, <see cref="minRank"/> to <see cref="maxRank"/>.</param>
    /// <param name="p_remove">Desired number of blank squares. Fewer are blanked when further removal would break uniqueness.</param>
    /// <param name="p_exhaustive">Skips the internal search budget, making the achieved blank count exact
    /// instead of near-maximal. Can run extremely long at high ranks with extreme blank counts
    /// (rank 5 max-blanks: upwards of half an hour where the budgeted run takes a minute).</param>
    /// <exception cref="ArgumentException">The rank is outside <see cref="minRank"/>..<see cref="maxRank"/>,
    /// or the requested blanks are negative.</exception>
    public static Sudoku Create(in int p_rank, in int p_remove, in bool p_exhaustive = false)
        => Create(p_rank, p_remove, new Random(), p_exhaustive);

    /// <summary>
    /// Seeded variant of Create: the same seed always reproduces the same sudoku.
    /// </summary>
    /// <param name="p_rank">Segment size of the sudoku, <see cref="minRank"/> to <see cref="maxRank"/>.</param>
    /// <param name="p_remove">Desired number of blank squares. Fewer are blanked when further removal would break uniqueness.</param>
    /// <param name="p_seed">Seed for the random generator. Determines the resulting sudoku together with <paramref name="p_exhaustive"/>.</param>
    /// <param name="p_exhaustive">Skips the internal search budget, making the achieved blank count exact
    /// instead of near-maximal. Can run extremely long at high ranks with extreme blank counts
    /// (rank 5 max-blanks: upwards of half an hour where the budgeted run takes a minute).
    /// The same seed can produce different puzzles budgeted vs exhaustive.</param>
    /// <exception cref="ArgumentException">The rank is outside <see cref="minRank"/>..<see cref="maxRank"/>,
    /// or the requested blanks are negative.</exception>
    public static Sudoku Create(in int p_rank, in int p_remove, in int p_seed, in bool p_exhaustive = false)
        => Create(p_rank, p_remove, new Random(p_seed), p_exhaustive);

    private static Sudoku Create(in int p_rank, in int p_remove, in Random p_random, in bool p_exhaustive)
    {
        Sudoku a_sudoku = new(p_rank, p_remove, p_random, p_exhaustive);
        a_sudoku.FillAll();
        a_sudoku.Prune();
        // a final shuffle washes out the positional bias of the backtracking fill
        return a_sudoku.Shuffle(p_random);
    }

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

    /// <summary>
    /// True when <see cref="puzzle"/> stays uniquely solvable after blanking <paramref name="p_idx"/>.
    /// The puzzle was unique before the removal, so a second solution would have to place another
    /// conflict-free candidate there; each is trialled for solvability under <see cref="trialBudget"/>.
    /// An exhausted trial counts as solvable, rejecting the removal — safe, but may undershoot max blanks.
    /// </summary>
    /// <param name="p_idx">Index of the square just blanked in <see cref="puzzle"/>.</param>
    /// <param name="p_removed">The <see cref="solution"/> value the square held before blanking.</param>
    /// <returns>True when no alternative value at <paramref name="p_idx"/> completes the puzzle.</returns>
    private bool UniqueWithout(in int p_idx, in int p_removed)
    {
        Copy(puzzle, altSolution);
        LoadMasks(altSolution);
        // captured before the trials below rebuild the masks
        uint a_usedMask = rowMask[rowOf[p_idx]] | colMask[colOf[p_idx]] | segMask[segOf[p_idx]];
        for (int a_input = 1; a_input <= rows; ++a_input)
        {
            if (a_input == p_removed || (a_usedMask & 1u << a_input) != 0) continue;
            altSolution[p_idx] = a_input;
            if (FillRemaining(altSolution, FillMode.NoInput, trialBudget)) return false;
            // a failed fill backtracks every square it set, so only p_idx needs resetting
            altSolution[p_idx] = 0;
        }
        return true;
    }

    /// <summary>
    /// Blanks random squares one at a time, keeping only removals that preserve uniqueness (via <see cref="UniqueWithout"/>), 
    /// until <see cref="remove"/> blanks are reached or no square can be removed.
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

    #endregion

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
    /// Recursive backtracker over the blank squares of <paramref name="p_arr"/>, always working on the most constrained blank first, 
    /// forced squares (one candidate left, or a hidden single) are placed without branching,
    /// and a grid is abandoned the moment any blank or needed unit value runs out of options.
    /// Deductions escalate lazily: only when nothing is forced are locked candidates derived
    /// (see <see cref="Derive_BlockEliminations"/>) and the scan repeated on the narrowed candidates,
    /// then naked pairs (see <see cref="Search_NakedPairs"/>) as the last rung before guessing,
    /// so easy grids never pay for the expensive deductions. All deductions keep every completion.
    /// When guessing is unavoidable, a two-homes value (see <see cref="FillValuePair"/>) is preferred over a square with 3+ candidates.
    /// <paramref name="p_mode"/> decides the candidate order per square.
    /// The masks must describe <paramref name="p_arr"/> on entry and are stale after a completed fill —
    /// enter via <see cref="FillRemaining"/>.
    /// </summary>
    /// <param name="p_arr">The grid to fill, <see cref="solution"/> or <see cref="altSolution"/>.</param>
    /// <param name="p_mode">Candidate order per square.</param>
    /// <returns>True when the grid was completed, or the node budget ran out (leaving <paramref name="p_arr"/>
    /// partially filled — see <see cref="FillRemaining"/>); a failed fill leaves it and the masks unchanged.</returns>
    private bool FillConstrained(in int[] p_arr, in FillMode p_mode)
    {
        // out of budget: give up, reporting the grid completable — the conservative answer for uniqueness trials
        if (--fillNodesLeft < 0) return true;
        int a_bestIdx = -1, a_bestCount = int.MaxValue;
        uint a_bestFreeMask = 0;
        if (!Scan_Candidates(p_arr, false, ref a_bestIdx, ref a_bestCount, ref a_bestFreeMask)) return false;
        if (a_bestIdx == -1) return true;
        if (a_bestCount > 1 && !Search_UnitForced(p_arr, ref a_bestIdx, ref a_bestCount, ref a_bestFreeMask)) return false;
        // nothing forced cheaply — escalate to locked candidates and rescan before resorting to guessing
        if (a_bestCount > 1)
        {
            Derive_BlockEliminations(p_arr);
            if (!Scan_Candidates(p_arr, true, ref a_bestIdx, ref a_bestCount, ref a_bestFreeMask)) return false;
            if (a_bestCount > 1 && !Search_UnitForced(p_arr, ref a_bestIdx, ref a_bestCount, ref a_bestFreeMask)) return false;
            // still nothing — escalate once more to naked pairs, rescanning only when they removed candidates
            if (a_bestCount > 1)
            {
                if (!Search_NakedPairs(p_arr, out bool a_eliminated)) return false;
                if (a_eliminated)
                {
                    if (!Scan_Candidates(p_arr, true, ref a_bestIdx, ref a_bestCount, ref a_bestFreeMask)) return false;
                    if (a_bestCount > 1 && !Search_UnitForced(p_arr, ref a_bestIdx, ref a_bestCount, ref a_bestFreeMask)) return false;
                }
            }
            // a value with exactly two homes in a unit is a narrower guess than a square with 3+ candidates
            if (a_bestCount > 2) for (int i = 0; i < rows; ++i)
            {
                uint a_pairMask = rowTwiceMask[i] & ~rowThriceMask[i];
                int[] a_unitOf = rowOf;
                if (a_pairMask == 0) { a_pairMask = colTwiceMask[i] & ~colThriceMask[i]; a_unitOf = colOf; }
                if (a_pairMask == 0) { a_pairMask = segTwiceMask[i] & ~segThriceMask[i]; a_unitOf = segOf; }
                if (a_pairMask == 0) continue;
                return FillValuePair(p_arr, p_mode, a_unitOf, i, a_pairMask & ~(a_pairMask - 1));
            }
        }
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
    /// One deduction pass over the blanks of <paramref name="p_arr"/>: refreshes <see cref="candMask"/>,
    /// rebuilds the once/twice/thrice unit masks and picks the most constrained square.
    /// Stops early on a naked single, leaving the unit masks incomplete — the callers' count guards cover that.
    /// </summary>
    /// <param name="p_arr">The grid being filled.</param>
    /// <param name="p_refine">False derives candidates from the unit masks; true narrows the stored ones
    /// by <see cref="rowBlockElim"/>/<see cref="colBlockElim"/> (see <see cref="Derive_BlockEliminations"/>).</param>
    /// <param name="p_bestIdx">Receives the most constrained blank square; -1 when the grid is full.</param>
    /// <param name="p_bestCount">Receives that square's candidate count.</param>
    /// <param name="p_bestMask">Receives that square's candidate mask.</param>
    /// <returns>False when some blank has no candidate left.</returns>
    private bool Scan_Candidates(in int[] p_arr, in bool p_refine, ref int p_bestIdx, ref int p_bestCount, ref uint p_bestMask)
    {
        p_bestIdx = -1; p_bestCount = int.MaxValue;
        for (int i = 0; i < rows; ++i)
        {
            rowOnceMask[i] = 0; colOnceMask[i] = 0; segOnceMask[i] = 0;
            rowTwiceMask[i] = 0; colTwiceMask[i] = 0; segTwiceMask[i] = 0;
            rowThriceMask[i] = 0; colThriceMask[i] = 0; segThriceMask[i] = 0;
        }
        for (int i = 0; i < squares; ++i)
        {
            if (p_arr[i] != 0) continue;
            int a_r = rowOf[i], a_c = colOf[i], a_s = segOf[i];
            uint a_freeMask = p_refine
                ? candMask[i] & ~rowBlockElim[rowBlockOf[i]] & ~colBlockElim[colBlockOf[i]]
                : ~(rowMask[a_r] | colMask[a_c] | segMask[a_s]) & fullMask;
            if (a_freeMask == 0) return false;
            candMask[i] = a_freeMask;
            int a_count = PopCount(a_freeMask);
            // values seen in one blank of the unit so far vs in two vs in more — singles are once & ~twice, pairs twice & ~thrice
            rowThriceMask[a_r] |= rowTwiceMask[a_r] & a_freeMask; rowTwiceMask[a_r] |= rowOnceMask[a_r] & a_freeMask; rowOnceMask[a_r] |= a_freeMask;
            colThriceMask[a_c] |= colTwiceMask[a_c] & a_freeMask; colTwiceMask[a_c] |= colOnceMask[a_c] & a_freeMask; colOnceMask[a_c] |= a_freeMask;
            segThriceMask[a_s] |= segTwiceMask[a_s] & a_freeMask; segTwiceMask[a_s] |= segOnceMask[a_s] & a_freeMask; segOnceMask[a_s] |= a_freeMask;
            if (a_count >= p_bestCount) continue;
            p_bestCount = a_count; p_bestIdx = i; p_bestMask = a_freeMask;
            // a forced square cannot be beaten, except by a dead one ending the fill anyway
            if (p_bestCount == 1) break;
        }
        return true;
    }

    /// <summary>
    /// Unit-level deductions over the masks of a completed <see cref="Scan_Candidates"/> pass: fails the
    /// fill when a unit can no longer host a value it still needs, and redirects the fill to a hidden
    /// single (a value with one possible square left in a unit) when one exists.
    /// </summary>
    /// <param name="p_arr">The grid being filled.</param>
    /// <param name="p_bestIdx">Updated to the hidden single's square when one is found.</param>
    /// <param name="p_bestCount">Updated to 1 when a hidden single is found.</param>
    /// <param name="p_bestMask">Updated to the hidden single's value bit when one is found.</param>
    /// <returns>False when a needed value has no square left in some unit.</returns>
    private bool Search_UnitForced(in int[] p_arr, ref int p_bestIdx, ref int p_bestCount, ref uint p_bestMask)
    {
        for (int i = 0; i < rows; ++i)
        {
            if ((fullMask & ~(rowMask[i] | rowOnceMask[i])) != 0) return false;
            if ((fullMask & ~(colMask[i] | colOnceMask[i])) != 0) return false;
            if ((fullMask & ~(segMask[i] | segOnceMask[i])) != 0) return false;
            uint a_hiddenMask = rowOnceMask[i] & ~rowTwiceMask[i];
            int[] a_unitOf = rowOf;
            if (a_hiddenMask == 0) { a_hiddenMask = colOnceMask[i] & ~colTwiceMask[i]; a_unitOf = colOf; }
            if (a_hiddenMask == 0) { a_hiddenMask = segOnceMask[i] & ~segTwiceMask[i]; a_unitOf = segOf; }
            if (a_hiddenMask == 0) continue;
            p_bestMask = a_hiddenMask & ~(a_hiddenMask - 1);
            p_bestIdx = Search_HiddenSingle(p_arr, a_unitOf, i, p_bestMask);
            p_bestCount = 1;
            break;
        }
        return true;
    }

    /// <summary>
    /// Locked candidates: summarises <see cref="candMask"/> per block (line∩segment intersection) and
    /// collects into <see cref="rowBlockElim"/>/<see cref="colBlockElim"/> the values each block cannot host:
    /// a value confined to one block of a segment stays out of the block's line elsewhere (pointing),
    /// one confined to one block of a line stays out of the block's segment elsewhere (claiming).
    /// </summary>
    /// <param name="p_arr">The grid being filled; <see cref="candMask"/> must describe its blanks.</param>
    private void Derive_BlockEliminations(in int[] p_arr)
    {
        for (int i = 0; i < squaresInSegmentRow; ++i)
        { rowBlockMask[i] = 0; colBlockMask[i] = 0; rowBlockElim[i] = 0; colBlockElim[i] = 0; }
        for (int i = 0; i < squares; ++i)
        {
            if (p_arr[i] != 0) continue;
            rowBlockMask[rowBlockOf[i]] |= candMask[i];
            colBlockMask[colBlockOf[i]] |= candMask[i];
        }
        // per line, the values confined to a single one of its blocks
        for (int i = 0; i < rows; ++i)
        {
            uint a_once = 0, a_twice = 0;
            for (int j = i * rank, a_limitIdx = j + rank; j < a_limitIdx; ++j)
            { a_twice |= a_once & rowBlockMask[j]; a_once |= rowBlockMask[j]; }
            rowConfMask[i] = a_once & ~a_twice;
            a_once = 0; a_twice = 0;
            for (int j = i * rank, a_limitIdx = j + rank; j < a_limitIdx; ++j)
            { a_twice |= a_once & colBlockMask[j]; a_once |= colBlockMask[j]; }
            colConfMask[i] = a_once & ~a_twice;
        }
        // band/stack from the loop counters, avoiding a per-segment div+mod by the runtime rank
        for (int a_bandIdx = 0; a_bandIdx < rank; ++a_bandIdx)
        for (int a_stackIdx = 0; a_stackIdx < rank; ++a_stackIdx)
        {
            uint a_once = 0, a_twice = 0;
            for (int j = 0; j < rank; ++j)
            {
                uint a_blockMask = rowBlockMask[(a_bandIdx * rank + j) * rank + a_stackIdx];
                a_twice |= a_once & a_blockMask; a_once |= a_blockMask;
            }
            uint a_segConfMask = a_once & ~a_twice;
            for (int a_lineIdx, a_blk, j = 0; j < rank; ++j)
            {
                a_lineIdx = a_bandIdx * rank + j; a_blk = a_lineIdx * rank + a_stackIdx;
                uint a_pointMask = rowBlockMask[a_blk] & a_segConfMask;
                uint a_claimMask = rowBlockMask[a_blk] & rowConfMask[a_lineIdx];
                if ((a_pointMask | a_claimMask) == 0) continue;
                for (int j2 = 0; j2 < rank; ++j2)
                {
                    if (j2 != a_stackIdx) rowBlockElim[a_lineIdx * rank + j2] |= a_pointMask;
                    if (j2 != j) rowBlockElim[(a_bandIdx * rank + j2) * rank + a_stackIdx] |= a_claimMask;
                }
            }
            a_once = 0; a_twice = 0;
            for (int j = 0; j < rank; ++j)
            {
                uint a_blockMask = colBlockMask[(a_stackIdx * rank + j) * rank + a_bandIdx];
                a_twice |= a_once & a_blockMask; a_once |= a_blockMask;
            }
            a_segConfMask = a_once & ~a_twice;
            for (int a_lineIdx, a_blk, j = 0; j < rank; ++j)
            {
                a_lineIdx = a_stackIdx * rank + j; a_blk = a_lineIdx * rank + a_bandIdx;
                uint a_pointMask = colBlockMask[a_blk] & a_segConfMask;
                uint a_claimMask = colBlockMask[a_blk] & colConfMask[a_lineIdx];
                if ((a_pointMask | a_claimMask) == 0) continue;
                for (int j2 = 0; j2 < rank; ++j2)
                {
                    if (j2 != a_bandIdx) colBlockElim[a_lineIdx * rank + j2] |= a_pointMask;
                    if (j2 != j) colBlockElim[(a_stackIdx * rank + j2) * rank + a_bandIdx] |= a_claimMask;
                }
            }
        }
    }

    /// <summary>
    /// Naked pairs: two blanks of a unit sharing the same two candidates lock those values between them,
    /// so both can be removed from every other square of the unit. Eliminations are applied directly to
    /// <see cref="candMask"/>; the caller rescans when any were made.
    /// </summary>
    /// <param name="p_arr">The grid being filled; <see cref="candMask"/> must describe its blanks.</param>
    /// <param name="p_eliminated">True when at least one candidate was removed.</param>
    /// <returns>False when an elimination leaves a square without candidates
    /// (e.g. a third square of the unit held the same bare pair).</returns>
    private bool Search_NakedPairs(in int[] p_arr, out bool p_eliminated)
    {
        p_eliminated = false;
        for (int i_unit = 0; i_unit < 3 * rows; ++i_unit)
        {
            int a_found = 0;
            for (int a_idx, a_baseIdx = i_unit * rows, k = 0; k < rows; ++k)
            {
                a_idx = unitSquares[a_baseIdx + k];
                if (p_arr[a_idx] != 0 || PopCount(candMask[a_idx]) != 2) continue;
                nakedSquares[a_found] = a_idx; nakedMasks[a_found] = candMask[a_idx]; ++a_found;
            }
            for (int a = 0; a < a_found; ++a)
                for (int b = a + 1; b < a_found; ++b)
                {
                    if (nakedMasks[a] != nakedMasks[b]) continue;
                    uint a_lockMask = nakedMasks[a];
                    for (int a_idx, a_baseIdx = i_unit * rows, k = 0; k < rows; ++k)
                    {
                        a_idx = unitSquares[a_baseIdx + k];
                        if (p_arr[a_idx] != 0 || a_idx == nakedSquares[a] || a_idx == nakedSquares[b]) continue;
                        uint a_newMask = candMask[a_idx] & ~a_lockMask;
                        if (a_newMask == candMask[a_idx]) continue;
                        if (a_newMask == 0) return false;
                        candMask[a_idx] = a_newMask; p_eliminated = true;
                    }
                }
        }
        return true;
    }

    /// <summary>
    /// Locates the one blank square of a unit that can still take the value of <paramref name="p_valueBit"/>.
    /// only called after the unit scan in <see cref="FillConstrained"/> proved exactly one such square exists.
    /// </summary>
    /// <param name="p_arr">The grid being filled.</param>
    /// <param name="p_unitOf">Square→unit lookup of the unit's type: <see cref="rowOf"/>, <see cref="colOf"/> or <see cref="segOf"/>.</param>
    /// <param name="p_unit">Index of the unit within its type.</param>
    /// <param name="p_valueBit">Bit of the hidden single's value.</param>
    /// <returns>Index of the found square.</returns>
    private int Search_HiddenSingle(in int[] p_arr, in int[] p_unitOf, in int p_unit, in uint p_valueBit)
    {
        for (int i = 0; i < squares; ++i)
            if (p_arr[i] == 0 && p_unitOf[i] == p_unit && (candMask[i] & p_valueBit) != 0)
                return i;
        throw new InvalidOperationException("hidden single not found, logic error");
    }

    /// <summary>
    /// Binary branch of <see cref="FillConstrained"/>: places the value of <paramref name="p_valueBit"/> in
    /// each of its two possible squares of a unit in turn and recurses — every completion must use one of them.
    /// In Uniqueness mode the square matching <see cref="solution"/> is tried last, preserving the steer-away order.
    /// </summary>
    /// <param name="p_arr">The grid being filled.</param>
    /// <param name="p_mode">Candidate order per square, passed through to the recursion.</param>
    /// <param name="p_unitOf">Square→unit lookup of the unit's type: <see cref="rowOf"/>, <see cref="colOf"/> or <see cref="segOf"/>.</param>
    /// <param name="p_unit">Index of the unit within its type.</param>
    /// <param name="p_valueBit">Bit of the value with exactly two possible squares in the unit.</param>
    /// <returns>True when the grid was completed; see <see cref="FillConstrained"/> for the budget caveat.</returns>
    private bool FillValuePair(in int[] p_arr, in FillMode p_mode, in int[] p_unitOf, in int p_unit, in uint p_valueBit)
    {
        int a_idx1 = -1, a_idx2 = -1;
        for (int i = 0; i < squares; ++i)
        {
            if (p_arr[i] != 0 || p_unitOf[i] != p_unit || (candMask[i] & p_valueBit) == 0) continue;
            if (a_idx1 == -1) a_idx1 = i; else { a_idx2 = i; break; }
        }
        if (a_idx2 == -1) throw new InvalidOperationException("value pair not found, logic error");
        int a_input = 0;
        for (uint i = p_valueBit; i > 1; i >>= 1) ++a_input;
        if (p_mode == FillMode.Uniqueness && solution[a_idx1] == a_input) (a_idx1, a_idx2) = (a_idx2, a_idx1);
        for (int a_idx = a_idx1, i = 0; i < 2; ++i, a_idx = a_idx2)
        {
            int a_row = rowOf[a_idx], a_col = colOf[a_idx], a_seg = segOf[a_idx];
            p_arr[a_idx] = a_input;
            rowMask[a_row] |= p_valueBit; colMask[a_col] |= p_valueBit; segMask[a_seg] |= p_valueBit;
            if (FillConstrained(p_arr, p_mode)) return true;
            rowMask[a_row] &= ~p_valueBit; colMask[a_col] &= ~p_valueBit; segMask[a_seg] &= ~p_valueBit;
            p_arr[a_idx] = 0;
        }
        return false;
    }

    /// <summary>
    /// Syncs the masks to <paramref name="p_arr"/> via <see cref="LoadMasks"/>, then runs <see cref="FillConstrained"/> over the whole grid.
    /// </summary>
    /// <param name="p_arr">The grid to fill, <see cref="solution"/> or <see cref="altSolution"/>.</param>
    /// <param name="p_mode">Candidate order per square.</param>
    /// <param name="p_nodeBudget">Search nodes the fill may spend; exhaustive when omitted. An exhausted fill
    /// returns true but leaves <paramref name="p_arr"/> partially filled — only safe on scratch grids (see <see cref="UniqueWithout"/>).</param>
    /// <returns>True when the grid was completed (or the budget ran out); a failed fill leaves <paramref name="p_arr"/> unchanged.</returns>
    private bool FillRemaining(in int[] p_arr, in FillMode p_mode, in int p_nodeBudget = int.MaxValue)
    {
        fillNodesLeft = p_nodeBudget;
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
