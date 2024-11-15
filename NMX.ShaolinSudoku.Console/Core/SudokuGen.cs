namespace NMX.ShaolinSudoku.Console.Core;
using Library.Core;
using System;

public static class SudokuGen
{
    private static Sudoku GetSudoku() => Sudoku.Create(3);
    private static Sudoku Find_Segment(this Sudoku p_sudoku, in int p_index)
    {
        int _startX = p_index / p_sudoku.rank * p_sudoku.rank, _startO = _startX - _startX / p_sudoku.rows % p_sudoku.rank * p_sudoku.rows;
        for (int i = 0; i < p_sudoku.rows; ++i) p_sudoku.solution[_startO + i / p_sudoku.rank * p_sudoku.rows + i % p_sudoku.rank] = 11;
        return p_sudoku;
    }
    private static void Print(this Sudoku p_sudoku, bool p_justIndexes = false)
    {
        void DrawLine() { Console.Write("\n  +"); for (int i = 0; i < p_sudoku.rows; ++i) Console.Write("----+"); }
        Console.Write("\t SUDOKU\n");
        DrawLine();
        for (int i = 0; i < p_sudoku.squareUnits; ++i)
        {
            if (i % p_sudoku.rows == 0) Console.Write("\n  ");
            Console.Write($"| {(p_justIndexes ? i : (int)p_sudoku.solution[i]):00} ");
            if (i % p_sudoku.rows + 1 == p_sudoku.rows) { Console.Write("|"); DrawLine(); }
        }
        Console.Write("\n");
    }
    public static void Main()
    {
        //GetSudoku().Find_Segment(37).Print();
        GetSudoku().Print();
    }
}
