namespace NMX.ShaolinSudoku.Console.Core;
using Library.Core;
using System;

public static class SudokuGen
{
    private static Sudoku GetSudoku() => Sudoku.Create(4, 181);
    private static void Print(this Sudoku p_sudoku, bool p_justIndexes = false)
    {
        void DrawLine() { Console.Write("\n  +"); for (int i = 0; i < p_sudoku.rows; ++i) Console.Write("----+"); }
        Console.Write("\t SUDOKU\n");
        DrawLine();
        for (int i = 0; i < p_sudoku.squares; ++i)
        {
            if (i % p_sudoku.rows == 0) Console.Write("\n  ");
            Console.Write($"| {(p_justIndexes ? i : p_sudoku.puzzle[i] == 0 ? "  " : p_sudoku.puzzle[i]):00} ");
            if (i % p_sudoku.rows + 1 == p_sudoku.rows) { Console.Write("|"); DrawLine(); }
        }
        Console.Write("\n");
    }
    public static void Main()
    {
        //GetSudoku().Print(true);
        GetSudoku().Print();
        //GetSudoku().Find_Segment(75).Print();

        //Sudoku _sudoku = GetSudoku();
        //_sudoku.Print();
        //_sudoku.Shuffle();
        //_sudoku.Print();
    }
}
