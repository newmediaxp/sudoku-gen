namespace NMX.ShaolinSudoku.Library.Core
{
    using System;
    using System.Collections.Generic;

    public sealed class Sudoku
    {
        public readonly int rank, remove, rows, squares;
        private readonly int segments, squaresInSegmentRow;
        public readonly int[] puzzle, solution;
        private readonly int[] rowInputs, altSol;
        private readonly List<int> inputs, shuffle;
        private static readonly Random random = new Random();

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
            rowInputs = new int[squares];
            altSol = new int[squares];
            inputs = new List<int>(rows);
            shuffle = new List<int>(rank);
        }
        public static Sudoku Create(in byte p_rank, in short p_remove)
        {
            Sudoku _sudoku = new Sudoku(p_rank, p_remove);
            _sudoku.Fill();
            _sudoku.Prune();
            //_sudoku.Shuffle();
            return _sudoku;
        }
        public static Sudoku Solve(in int[] p_puzz)
        {
            int _rank = (int)Math.Sqrt(Math.Sqrt(p_puzz.Length));
            if (_rank * _rank * _rank * _rank == p_puzz.Length) throw new InvalidOperationException("invalid puzzle length");
            Sudoku _sudoku = new Sudoku(_rank, Count(p_puzz, 0));
            return _sudoku;
        }
        private static int Count(in int[] p_arr, in int p_input)
        { int _count = 0; for (int i = 0; i < p_arr.Length; ++i) if (p_arr[i] == p_input) ++_count; return _count; }
        private static bool Same(in int[] p_arr1, in int[] p_arr2)
        { for (int i = 0; i < p_arr1.Length; ++i) if (p_arr1[i] != p_arr2[i]) return false; return true; }
        private static void Swap3(in int[] p_arr, in int p_i1, in int p_i2, in int p_i3)
        { int _temp = p_arr[p_i1]; p_arr[p_i1] = p_arr[p_i2]; p_arr[p_i2] = p_arr[p_i3]; p_arr[p_i3] = _temp; }
        private int Row(in int p_idx) => p_idx / rows;
        private int Idx_Row(in int p_row, in int p_pos) => p_row * rows + p_pos;
        private int Col(in int p_idx) => p_idx % rows;
        private int Idx_Col(in int p_col, in int p_pos) => p_pos * rows + p_col;
        private int Seg(in int p_idx) => Row(p_idx) / rank * rank + Col(p_idx) / rank;
        private int Idx_Seg(in int p_seg, in int p_pos) => p_seg / rank * squaresInSegmentRow + p_seg % rank * rank
            + p_pos / rank * rows + p_pos % rank;
        private void Init(in int[] p_arr) { for (int i = 0; i <= p_arr.Length; ++i) p_arr[i] = 0; }
        private void Init(in List<int> p_list)
        {
            p_list.Clear(); bool isInputs = p_list.Capacity == rows;
            for (int i = 0; i < p_list.Capacity; ++i) p_list.Add(isInputs ? i + 1 : i);
        }
        public int PopRandom(in List<int> p_list)
        { int i = random.Next(p_list.Count); int _num = p_list[i]; p_list.RemoveAt(i); return _num; }
        private void InitRandom(in int[] p_arr)
        { for (int i = 0; i < p_arr.Length; ++i) { if (inputs.Count == 0) Init(inputs); p_arr[i] = PopRandom(inputs); } }
        private bool Search_RowCol(in int[] p_arr, in int p_input, in int p_idx, in bool p_includeIdx)
        {
            if (p_includeIdx && p_arr[p_idx] == p_input) return true;
            for (int _row = Row(p_idx), _col = Col(p_idx), i_row, i_col, i = 0; i < rows; ++i)
            {
                i_row = Idx_Row(_row, i); i_col = Idx_Col(_col, i);
                if (!p_includeIdx && (i_row == p_idx || i_col == p_idx)) continue;
                if (p_arr[i_row] == p_input || p_arr[i_col] == p_input) return true;
            }
            return false;
        }
        private bool Search_Segment(in int[] p_arr, in int p_input, in int p_idx, in bool p_includeIdx)
        {
            if (p_includeIdx && p_arr[p_idx] == p_input) return true;
            for (int _seg = Seg(p_idx), i_seg, i = 0; i < rows; ++i)
            {
                i_seg = Idx_Seg(_seg, i); if (!p_includeIdx && i_seg == p_idx) continue;
                if (p_arr[Idx_Seg(_seg, i)] == p_input) return true;
            }
            return false;
        }
        private void Fill_DiagonalSegments(in int[] p_arr)
        {
            for (int i = 0; i < squaresInSegmentRow; ++i)
            { if (inputs.Count == 0) Init(inputs); p_arr[Idx_Seg(i / rows * (rank + 1), i % rows)] = PopRandom(inputs); }
        }
        private void Shuffle(in int[] p_arr)    // TODO : Convert to Idx_()
        {
            int _posA, _posB;
            List<int> _shuffle = new List<int>(rank); ;
            // swapping segment rows
            Init(_shuffle); _posA = PopRandom(_shuffle); _posB = PopRandom(_shuffle);
            for (int i = 0; i < squaresInSegmentRow; ++i) Swap3(p_arr, _shuffle[0] * squaresInSegmentRow + i,
                _posA * squaresInSegmentRow + i, _posB * squaresInSegmentRow + i);
            // swapping segment columns
            Init(_shuffle); _posA = PopRandom(_shuffle); _posB = PopRandom(_shuffle);
            for (int i = 0; i < squaresInSegmentRow; ++i) Swap3(p_arr, _shuffle[0] * rank + i / rows + i % rows * rows,
                _posA * rank + i / rows + i % rows * rows, _posB * rank + i / rows + i % rows * rows);
            // swapping within segment rows and columns
            for (int i_seg = 0; i_seg < rank; ++i_seg)
            {
                Init(_shuffle); _posA = PopRandom(_shuffle); _posB = PopRandom(_shuffle);
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
        private bool SequentialFill_Remaining(in int[] p_arr, in int[] p_inputs, int p_idx)
        {
            if (!(p_idx < squares)) return true;
            while (p_arr[p_idx] != 0) { ++p_idx; if (!(p_idx < squares)) return true; }
            for (int _input, _row = Row(p_idx), _col = Col(p_idx), i = 0; i < rows; ++i)
            {
                _input = p_inputs[Idx_Row(_row, (_col + 1 + i) % rows)];
                if (Search_RowCol(p_arr, _input, p_idx, true) || Search_Segment(p_arr, _input, p_idx, true)) continue;
                p_arr[p_idx] = _input; if (SequentialFill_Remaining(p_arr, p_inputs, p_idx + 1)) return true;
                p_arr[p_idx] = 0;
            }
            return false;
        }
        private bool Fill_Remaining(in int[] p_arr, in int[] p_inputs) => SequentialFill_Remaining(p_arr, p_inputs, 0);
        private void Fill()
        {
            Fill_DiagonalSegments(solution);
            InitRandom(rowInputs);
            Fill_Remaining(solution, rowInputs);
        }
        private bool Unique() { Fill_Remaining(altSol, solution); return Same(altSol, solution); }
        private void Prune()
        {
            Buffer.BlockCopy(solution, 0, puzzle, 0, Buffer.ByteLength(puzzle));
            Buffer.BlockCopy(solution, 0, altSol, 0, Buffer.ByteLength(altSol));
            int _idx, _input, _removed = 0;
            List<int> _removable = new List<int>(squares); Init(_removable);
            while (_removable.Count > 0 && _removed < remove)
            {
                _idx = PopRandom(_removable); _input = puzzle[_idx]; if (_input == 0) continue;
                puzzle[_idx] = altSol[_idx] = 0; if (Unique()) { ++_removed; continue; }
                puzzle[_idx] = altSol[_idx] = _input;
            }
        }
        private void Shuffle() => Shuffle(solution);
    }
}
