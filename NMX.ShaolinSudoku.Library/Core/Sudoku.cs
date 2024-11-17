namespace NMX.ShaolinSudoku.Library.Core
{
    using System;
    using System.Collections.Generic;

    public sealed class Sudoku
    {
        public readonly int rank, rows, squres;
        private readonly int segments, squaresInSegmentRow;
        public readonly int[] puzzle, solution;
        private readonly int[] rowInputs;
        private readonly List<int> inputs, shuffle, removed;
        private static readonly Random random = new Random();

        private Sudoku(in int p_rank)
        {
            rank = p_rank;
            rows = rank * rank;
            segments = rows;
            squres = rows * rows;
            squaresInSegmentRow = segments * rank;
            puzzle = new int[squres];
            solution = new int[squres];
            rowInputs = new int[squres];
            inputs = new List<int>(segments);
            shuffle = new List<int>(rank);
            removed = new List<int>(squres);
        }
        public static Sudoku Create(in byte p_rank)
        {
            Sudoku _sudoku = new Sudoku(p_rank);
            _sudoku.Fill();
            return _sudoku;
        }
        private void Fill()
        {
            Fill_DiagonalSegments(solution);
        }
        public void Shuffle()
        {
            Shuffle(solution);
        }
        private void Init_arr(in int[] p_arr) { for (int i = 0; i <= p_arr.Length; ++i) p_arr[i] = 0; }
        private void Init_inputs() { inputs.Clear(); for (int i = 1; i <= inputs.Capacity; ++i) inputs.Add(i); }
        private void Init_shuffle() { shuffle.Clear(); for (int i = 0; i < shuffle.Capacity; ++i) shuffle.Add(i); }
        private void Init_removed() { removed.Clear(); for (int i = 0; i < removed.Capacity; ++i) removed.Add(i); }
        public int RandomPop(in List<int> p_list)
        { int i = random.Next(p_list.Count); int _num = p_list[i]; p_list.RemoveAt(i); return _num; }
        private void Swap3(in int[] p_arr, in int p_i1, in int p_i2, in int p_i3)
        { int _temp = p_arr[p_i1]; p_arr[p_i1] = p_arr[p_i2]; p_arr[p_i2] = p_arr[p_i3]; p_arr[p_i3] = _temp; }
        private void Init_rowInputs()
        {
            for (int i = 0; i < rowInputs.Length; ++i)
            { if (inputs.Count == 0) Init_inputs(); rowInputs[i] = RandomPop(inputs); }
        }
        private bool Search_RowCol(in int[] p_arr, in int p_input, in int p_index)
        {
            int _startRow = p_index / rows, _endRow = _startRow + rows;
            for (int i = _startRow; i < _endRow && i < squres; ++i) if (p_arr[i] == p_input) return true;
            int _startCol = p_index % rows;
            for (int i = _startCol; i < squres; i += rows) if (p_arr[i] == p_input) return true;
            return false;
        }
        private bool Search_Segment(in int[] p_arr, in int p_input, in int p_index)
        {
            int _startX = p_index / rank * rank, _startO = _startX - _startX / rows % rank * rows;
            for (int i = 0; i < rows; ++i) if (p_arr[_startO + i / rank * rows + i % rank] == p_input) return true;
            return false;
        }
        private void Fill_DiagonalSegments(in int[] p_arr)
        {
            for (int i = 0; i < squaresInSegmentRow; ++i)
            { if (inputs.Count == 0) Init_inputs(); p_arr[i / rows * rank + i / rank * rows + i % rank] = RandomPop(inputs); }
        }
        private void Shuffle(in int[] p_arr)
        {
            int _posA, _posB;
            // swapping segment rows
            Init_shuffle(); _posA = RandomPop(shuffle); _posB = RandomPop(shuffle);
            for (int i = 0; i < squaresInSegmentRow; ++i) Swap3(p_arr,shuffle[0] * squaresInSegmentRow + i,
                _posA * squaresInSegmentRow + i, _posB * squaresInSegmentRow + i);
            // swapping segment columns
            Init_shuffle(); _posA = RandomPop(shuffle); _posB = RandomPop(shuffle);
            for (int i = 0; i < squaresInSegmentRow; ++i) Swap3(p_arr,shuffle[0] * rank + i / rows + i % rows * rows,
                _posA * rank + i / rows + i % rows * rows, _posB * rank + i / rows + i % rows * rows);
            // swapping within segment rows and columns
            for (int i_seg = 0; i_seg < rank; ++i_seg)
            {
                Init_shuffle(); _posA = RandomPop(shuffle); _posB = RandomPop(shuffle);
                for (int i = 0; i < rows; ++i)
                {
                    // swapping rows in segment rows
                    Swap3(p_arr,i_seg * squaresInSegmentRow + shuffle[0] * rows + i,
                        i_seg * squaresInSegmentRow + _posA * rows + i, i_seg * squaresInSegmentRow + _posB * rows + i);
                    // swapping columns in segment columns
                    Swap3(p_arr, i_seg * rank + shuffle[0] + i * rows,
                        i_seg * rank + _posA + i * rows, i_seg * rank + _posB + i * rows);
                }
            }
        }

    }
}
