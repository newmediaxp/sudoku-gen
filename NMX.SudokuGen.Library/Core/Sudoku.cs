namespace NMX.SudokuGen.Library.Core
{
    using System;
    using static Utility;

    public sealed class Sudoku
    {
        public readonly int rank, remove, rows, squares;
        private readonly int squaresInSegmentRow;
        public readonly int[] puzzle, solution;
        private readonly int[] altSol;
        public int Removed { get; private set; }
        private enum FillMode : byte { NoInput, RandomRow, Uniqueness }

        private Sudoku(in int p_rank, in int p_remove)
        {
            rank = p_rank;
            remove = p_remove;
            rows = rank * rank;
            squares = rows * rows;
            squaresInSegmentRow = rows * rank;
            puzzle = new int[squares];
            solution = new int[squares];
            altSol = new int[squares];
        }
        public static Sudoku Create(int p_rank, int p_remove)
        {
            Sudoku a_sudoku = new Sudoku(p_rank, p_remove);
            a_sudoku.FillAll();
            a_sudoku.Shuffle();
            a_sudoku.Prune();
            return a_sudoku;
        }
        public static Sudoku Solve(int[] p_puzz)
        {
            int a_rank = (int)Math.Sqrt(Math.Sqrt(p_puzz.Length));
            if (a_rank * a_rank * a_rank * a_rank != p_puzz.Length)
                throw new InvalidOperationException("invalid puzzle length, could not determine rank");
            Sudoku a_sudoku = new Sudoku(a_rank, Count(p_puzz, 0));
            a_sudoku.Removed = a_sudoku.remove;
            if (!a_sudoku.Valid(p_puzz)) throw new InvalidOperationException("invalid puzzle, duplicate inputs found");
            Copy(p_puzz, a_sudoku.puzzle); Copy(a_sudoku.puzzle, a_sudoku.solution);
            a_sudoku.FillRest();
            if (!a_sudoku.Unique()) throw new InvalidOperationException("invalid puzzle, no unique solution");
            return a_sudoku;
        }
        private void Shuffle3()
        {
            const int a_rank = 3, a_rows = a_rank * a_rank, a_squaresInSegmentRow = a_rows * a_rank;
            int[] a_idxs = new int[a_rank];
            // swapping segment rows
            InitRandom(a_idxs, a_rank, false);
            for (int i = 0; i < a_squaresInSegmentRow; ++i)
            {
                Swap3(ref puzzle[a_idxs[0] * a_squaresInSegmentRow + i],
                    ref puzzle[a_idxs[1] * a_squaresInSegmentRow + i],
                    ref puzzle[a_idxs[2] * a_squaresInSegmentRow + i]);
                Swap3(ref solution[a_idxs[0] * a_squaresInSegmentRow + i],
                    ref solution[a_idxs[1] * a_squaresInSegmentRow + i],
                    ref solution[a_idxs[2] * a_squaresInSegmentRow + i]);
            }
            // swapping segment columns
            InitRandom(a_idxs, a_rank, false);
            for (int i = 0; i < a_squaresInSegmentRow; ++i)
            {
                Swap3(ref puzzle[a_idxs[0] * a_rank + i / a_rows + i % a_rows * a_rows],
                    ref puzzle[a_idxs[1] * a_rank + i / a_rows + i % a_rows * a_rows],
                    ref puzzle[a_idxs[2] * a_rank + i / a_rows + i % a_rows * a_rows]);
                Swap3(ref solution[a_idxs[0] * a_rank + i / a_rows + i % a_rows * a_rows],
                    ref solution[a_idxs[1] * a_rank + i / a_rows + i % a_rows * a_rows],
                    ref solution[a_idxs[2] * a_rank + i / a_rows + i % a_rows * a_rows]);
            }
            // swapping rows in segment rows
            for (int i_seg = 0; i_seg < a_rank; ++i_seg)
            {
                InitRandom(a_idxs, a_rank, false);
                for (int i = 0; i < a_rows; ++i)
                {
                    Swap3(ref puzzle[i_seg * a_squaresInSegmentRow + a_idxs[0] * a_rows + i],
                        ref puzzle[i_seg * a_squaresInSegmentRow + a_idxs[1] * a_rows + i],
                        ref puzzle[i_seg * a_squaresInSegmentRow + a_idxs[2] * a_rows + i]);
                    Swap3(ref solution[i_seg * a_squaresInSegmentRow + a_idxs[0] * a_rows + i],
                        ref solution[i_seg * a_squaresInSegmentRow + a_idxs[1] * a_rows + i],
                        ref solution[i_seg * a_squaresInSegmentRow + a_idxs[2] * a_rows + i]);
                }
            }
            // swapping columns in segment columns
            for (int i_seg = 0; i_seg < a_rank; ++i_seg)
            {
                InitRandom(a_idxs, a_rank, false);
                for (int i = 0; i < a_rows; ++i)
                {
                    Swap3(ref puzzle[i_seg * a_rank + a_idxs[0] + i * a_rows],
                        ref puzzle[i_seg * a_rank + a_idxs[1] + i * a_rows],
                        ref puzzle[i_seg * a_rank + a_idxs[2] + i * a_rows]);
                    Swap3(ref solution[i_seg * a_rank + a_idxs[0] + i * a_rows],
                        ref solution[i_seg * a_rank + a_idxs[1] + i * a_rows],
                        ref solution[i_seg * a_rank + a_idxs[2] + i * a_rows]);
                }
            }
        }
        public bool Search_RowCol(in int[] p_arr, in int p_input, in int p_idx)
        {
            for (int a_startRow = p_idx / rows * rows, a_endRow = a_startRow + rows,
                i = a_startRow; i < a_endRow && i < squares; ++i) if (i != p_idx && p_arr[i] == p_input) return true;
            for (int a_startCol = p_idx % rows,
                i = a_startCol; i < squares; i += rows) if (i != p_idx && p_arr[i] == p_input) return true;
            return false;
        }
        public bool Search_Segment(in int[] p_arr, in int p_input, in int p_idx)
        {
            for (int a_startX = p_idx / rank * rank, a_startO = a_startX - a_startX / rows % rank * rows,
                a_o, i = 0; i < rows; ++i)
            {
                a_o = a_startO + i / rank * rows + i % rank;
                if (a_o != p_idx && p_arr[a_o] == p_input) return true;
            }
            return false;
        }
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
        private void Set_DiagonalSegments(in int[] p_arr)
        {
            int[] a_inputs = new int[squaresInSegmentRow];
            InitRandom(a_inputs, rows, true);
            for (int i = 0; i < squaresInSegmentRow; ++i)
                p_arr[i / rows * rank + i / rank * rows + i % rank] = a_inputs[i];
        }
        // optimised for performance boost, reduced branches
        // num is int, { num >> 31 } gives -1 (when num < 0) or 0 (otherwise)  
        //private int FillSequential(in FillMode p_mode, int p_idx) 
        //{
        //    if (!(p_idx < squares)) return 1;
        //    int[] _arr = p_mode == FillMode.Uniqueness ? altSol : solution;
        //    while (p_idx < squares && _arr[p_idx] != 0) ++p_idx;
        //    if (!(p_idx < squares)) return 1;
        //    int _return = 0;
        //    for (int _startRow = p_idx / rows * rows, _input, i = 0; i < rows && _return == 0; ++i)
        //    {
        //        if (p_mode == FillMode.NoInput) _input = i + 1;
        //        else if (p_mode == FillMode.RandomRow) _input = altSol[_startRow + i];
        //        else if (p_mode == FillMode.Uniqueness)
        //        { _input = solution[p_idx] + i + 1; _input += ((rows - _input) >> 31) * rows; }
        //        else throw new InvalidOperationException("unknown fill mode");
        //        if (Search_RowCol(_arr, _input, p_idx) || Search_Segment(_arr, _input, p_idx)) continue;
        //        _arr[p_idx] = _input;
        //        _arr[p_idx] = (_return = FillSequential(p_mode, p_idx + 1)) * _input;
        //    }
        //    return _return;
        //}
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
        private void FillAll()
        {
            if (rank >= 3) Set_DiagonalSegments(solution);
            InitRandom(altSol, rows, true);
            FillRemaining(FillMode.RandomRow);
            if (Count(solution, 0) > 0) throw new InvalidOperationException("cannot create, logic error");
        }
        private void FillRest()
        {
            FillRemaining(FillMode.NoInput);
            if (Count(solution, 0) > 0) throw new InvalidOperationException("cannot solve, logic error");
        }
        private bool Unique()
        {
            Copy(puzzle, altSol);
            FillRemaining(FillMode.Uniqueness); return Same(altSol, solution);
        }
        private void Prune()
        {
            int[] a_indexes = new int[squares];
            InitRandom(a_indexes, squares, false);
            Copy(solution, puzzle); Removed = 0;
            for (int a_input, i = 0; i < a_indexes.Length && Removed < remove; ++i)
            {
                a_input = puzzle[a_indexes[i]]; if (a_input == 0) continue;
                puzzle[a_indexes[i]] = 0; if (Unique()) { ++Removed; continue; }
                puzzle[a_indexes[i]] = a_input;
            }
        }
        private void Shuffle() 
        { 
            //if (rank == 3) Shuffle3();
        }
    }
}
