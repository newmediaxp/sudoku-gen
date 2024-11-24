namespace NMX.ShaolinSudoku.Library.Core
{
    using System;
    using System.Collections.Generic;
    using static Utility;

    public sealed class Sudoku
    {
        public readonly int rank, remove, rows, squares;
        private readonly int segments, squaresInSegmentRow;
        public readonly int[] puzzle, solution;
        private readonly int[] altSol;

        private enum FillMode { NoInput, RandomRow, Uniqueness }
        public int Removed { get; private set; }

        private Sudoku(in int p_rank, in int p_remove)
        {
            rank = p_rank;
            remove = p_remove;
            rows = rank * rank;
            segments = rows;
            squares = rows * rows;
            squaresInSegmentRow = segments * rank;
            puzzle = new int[squares];
            solution = new int[squares];
            altSol = new int[squares];
        }
        public static Sudoku Create(in byte p_rank, in short p_remove)
        {
            Sudoku _sudoku = new Sudoku(p_rank, p_remove); _sudoku.FillAll();
            /*_sudoku.Shuffle();*/ _sudoku.Prune();
            return _sudoku;
        }
        public static Sudoku Solve(in int[] p_puzz)
        {
            int _rank = (int)Math.Sqrt(Math.Sqrt(p_puzz.Length));
            if (_rank * _rank * _rank * _rank != p_puzz.Length)
                throw new InvalidOperationException("invalid puzzle length, could not determine rank");
            Sudoku _sudoku = new Sudoku(_rank, Count(p_puzz, 0)); _sudoku.Removed = _sudoku.remove;
            if (!_sudoku.Valid(p_puzz)) throw new InvalidOperationException("invalid puzzle, duplicate inputs found");
            Copy(p_puzz, _sudoku.puzzle); Copy(_sudoku.puzzle, _sudoku.solution); _sudoku.FillRest();
            if (!_sudoku.Unique()) throw new InvalidOperationException("invalid puzzle, no unique solution");
            return _sudoku;
        }
        private void Shuffle(in int[] p_arr)
        {
            int _posA, _posB;
            List<int> _shuffle = new List<int>(rank);
            // swapping segment rows
            Init(_shuffle, false); _posA = PopRandom(_shuffle); _posB = PopRandom(_shuffle);
            for (int i = 0; i < squaresInSegmentRow; ++i) Swap3(p_arr, _shuffle[0] * squaresInSegmentRow + i,
                _posA * squaresInSegmentRow + i, _posB * squaresInSegmentRow + i);
            // swapping segment columns
            Init(_shuffle, false); _posA = PopRandom(_shuffle); _posB = PopRandom(_shuffle);
            for (int i = 0; i < squaresInSegmentRow; ++i) Swap3(p_arr, _shuffle[0] * rank + i / rows + i % rows * rows,
                _posA * rank + i / rows + i % rows * rows, _posB * rank + i / rows + i % rows * rows);
            // swapping within segment rows and columns
            for (int i_seg = 0; i_seg < rank; ++i_seg)
            {
                Init(_shuffle, false); _posA = PopRandom(_shuffle); _posB = PopRandom(_shuffle);
                for (int i = 0; i < rows; ++i)
                {
                    // swapping rows in segment rows
                    Swap3(p_arr, i_seg * squaresInSegmentRow + _shuffle[0] * rows + i,
                        i_seg * squaresInSegmentRow + _posA * rows + i, i_seg * squaresInSegmentRow + _posB * rows + i);
                    // swapping columns in segment columns
                    Swap3(p_arr, i_seg * rank + _shuffle[0] + i * rows,
                        i_seg * rank + _posA + i * rows, i_seg * rank + _posB + i * rows);
                }
            }
        }
        private bool Search_RowCol(in int[] p_arr, in int p_input, in int p_idx)
        {
            for (int _startRow = p_idx / rows * rows, _endRow = _startRow + rows,
                i = _startRow; i < _endRow && i < squares; ++i) if (i != p_idx && p_arr[i] == p_input) return true;
            for (int _startCol = p_idx % rows,
                i = _startCol; i < squares; i += rows) if (i != p_idx && p_arr[i] == p_input) return true;
            return false;
        }
        private bool Search_Segment(in int[] p_arr, in int p_input, in int p_idx)
        {
            for (int _startX = p_idx / rank * rank, _startO = _startX - _startX / rows % rank * rows,
                _o, i = 0; i < rows; ++i)
            {
                _o = _startO + i / rank * rows + i % rank;
                if (_o != p_idx && p_arr[_o] == p_input) return true;
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
            List<int> _inputs = new List<int>(rows); Init(_inputs, true);
            for (int i = 0; i < squaresInSegmentRow; ++i)
            { if (_inputs.Count == 0) Init(_inputs, true); p_arr[i / rows * rank + i / rank * rows + i % rank] = PopRandom(_inputs); }
        }
        private bool FillSequential(in FillMode p_mode, int p_idx)
        {
            if (!(p_idx < squares)) return true;
            int[] _arr = p_mode == FillMode.Uniqueness ? altSol : solution;
            while (_arr[p_idx] != 0) { ++p_idx; if (!(p_idx < squares)) return true; }
            for (int _input, _startRow = p_idx / rows * rows, i = 0; i < rows; ++i)
            {
                if (p_mode == FillMode.NoInput) _input = i + 1;
                else if (p_mode == FillMode.RandomRow) _input = altSol[_startRow + i];
                else if (p_mode == FillMode.Uniqueness) { _input = solution[p_idx] + i + 1; if (_input > rows) _input -= rows; }
                else _input = 0;
                if (Search_RowCol(_arr, _input, p_idx) || Search_Segment(_arr, _input, p_idx)) continue;
                _arr[p_idx] = _input; if (FillSequential(p_mode, p_idx + 1)) return true;
                _arr[p_idx] = 0;
            }
            return false;
        }
        private void FillAll()
        {
            Set_DiagonalSegments(solution); InitRandom(altSol, new List<int>(rows), true);
            FillSequential(FillMode.RandomRow, 0);
            if (Count(solution, 0) > 0) throw new InvalidOperationException("cannot create, logic error");
        }
        private void FillRest()
        {
            FillSequential(FillMode.NoInput, 0);
            if (Count(solution, 0) > 0) throw new InvalidOperationException("cannot solve, logic error");
        }
        private bool Unique()
        {
            Copy(puzzle, altSol);
            FillSequential(FillMode.Uniqueness, 0); return Same(altSol, solution);
        }
        private void Prune()
        {
            int _idx, _input;
            List<int> _removable = new List<int>(squares); Init(_removable, false);
            Copy(solution, puzzle); Removed = 0;
            while (_removable.Count > 0 && Removed < remove)
            {
                _idx = PopRandom(_removable); _input = puzzle[_idx]; if (_input == 0) continue;
                puzzle[_idx] = 0; if (Unique()) { ++Removed; continue; }
                puzzle[_idx] = _input;
            }
        }
        private void Shuffle() => Shuffle(solution);
    }
}
