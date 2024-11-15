namespace NMX.ShaolinSudoku.Library.Core
{
    using System;
    using System.Collections.Generic;

    public sealed class Sudoku
    {
        public readonly int rank, rows, squareUnits;
        private readonly int squareSegments, squareUnitsInDiagonalSquareSegments;
        public readonly uint[] puzzle, solution;
        private readonly uint[] rowInputs;
        private readonly List<uint> inputs, shuffle, removed;
        private readonly Random random = new Random();
        private Sudoku(in int p_rank)
        {
            rank = p_rank;
            rows = rank * rank;
            squareSegments = rows;
            squareUnits = rows * rows;
            squareUnitsInDiagonalSquareSegments = squareSegments * rank;
            puzzle = new uint[squareUnits];
            solution = new uint[squareUnits];
            rowInputs = new uint[squareUnits];
            inputs = new List<uint>(squareSegments);
            shuffle = new List<uint>(rank);
            removed = new List<uint>(squareUnits);
        }
        public static Sudoku Create(in byte p_rank)
        {
            Sudoku _sudoku = new Sudoku(p_rank);
            _sudoku.Fill_DiagonalSquareSegments();
            return _sudoku;
        }
        private void Init_inputs() { inputs.Clear(); for (uint i = 1; i <= inputs.Capacity; ++i) inputs.Add(i); }
        private void Init_shuffle() { shuffle.Clear(); for (uint i = 0; i < shuffle.Capacity; ++i) shuffle.Add(i); }
        private void Init_removed() { removed.Clear(); for (uint i = 0; i < removed.Capacity; ++i) removed.Add(i); }
        private uint RandomPop(in List<uint> p_list)
        { int i = random.Next(p_list.Count); uint _num = p_list[i]; p_list.RemoveAt(i); return _num; }
        private void Init_rowInputs()
        {
            for (int i = 0; i < rowInputs.Length; ++i)
            { if (inputs.Count == 0) Init_inputs(); rowInputs[i] = RandomPop(inputs); }
        }
        private bool Search_RowCol(in uint[] p_arr, in uint p_input, in int p_index)
        {
            int _startRow = p_index / rows, _endRow = _startRow + rows;
            for (int i = _startRow; i < _endRow && i < p_arr.Length; ++i) if (p_arr[i] == p_input) return true;
            int _startCol = p_index % rows;
            for (int i = _startCol; i < p_arr.Length; i += rows) if (p_arr[i] == p_input) return true;
            return false;
        }
        private bool Search_Segment(in uint[] p_arr, in uint p_input, in int p_index)
        {
            int _startX = p_index / rank * rank, _startO = _startX - _startX / rows % rank * rows;
            for (int i = 0; i < rows; ++i) if (solution[_startO + i / rank * rows + i % rank] == p_input) return true;
            return false;
        }
        private void Fill_DiagonalSquareSegments()
        {
            for (int i = 0; i < squareUnitsInDiagonalSquareSegments; ++i)
            { if (inputs.Count == 0) Init_inputs(); solution[i / rows * rank + i / rank * rows + i % rank] = RandomPop(inputs); }
        }
    }
}
