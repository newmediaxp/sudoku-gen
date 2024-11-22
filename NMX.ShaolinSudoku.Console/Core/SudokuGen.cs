namespace NMX.ShaolinSudoku.Console.Core;
using Library.Core;
using System;
using System.Timers;

public static class SudokuGen
{
    private static void PrintFormatted(this Sudoku p_sudoku, bool p_justIndexes = false)
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
    private static void Print(this Sudoku p_sudoku)
    {
        Console.Write($"+Sudoku>\tRank={p_sudoku.rank}\tGiven={p_sudoku.squares - p_sudoku.Removed}");
        Console.Write("\n|  Puzz: "); for (int i = 0; i < p_sudoku.puzzle.Length; ++i) Console.Write($"{p_sudoku.puzzle[i]},");
        Console.Write("\n|  Soln: "); for (int i = 0; i < p_sudoku.solution.Length; ++i) Console.Write($"{p_sudoku.solution[i]},");
        Console.WriteLine();
    }
    private static void Test()
    {
        Sudoku.Create(3, 3*3*3*3).Print();
        //Sudoku.Create(3, 80).PrintFormatted();
        ////---
        //        int[] _puzz = new int[81]
        //        {
        //3,7,0,0,0,3,0,0,6,0,1,0,2,5,0,0,0,0,0,4,2,0,0,0,0,0,2,0,0,0,2,0,5,0,0,0,0,0,0,0,9,0,0,0,8,4,0,1,7,0,3,5,0,0,9,0,0,0,1,4,8,6,0,0,6,0,0,0,0,0,5,0,0,0,0,0,0,0,1,0,9,
        //        };
        //        try { Sudoku.Solve(_puzz).Print(); }
        //        catch (Exception p_ex) { Console.WriteLine($"Error: {p_ex.Message}"); }
        //---
    }
    private static void TestTimes()
    {
        DateTime old_start = DateTime.Now;
        new SudokuOperations().GetSudoku(80);
        DateTime old_end = DateTime.Now;
        Console.WriteLine($"OldGen :\t{(old_end - old_start).TotalMilliseconds} ms");
        DateTime new_start = DateTime.Now;
        Sudoku.Create(3, 80);
        DateTime new_end = DateTime.Now;
        Console.WriteLine($"NewGen :\t{(new_end - new_start).TotalMilliseconds} ms");
    }
    public static void Main()
        //=> Test();
    => TestTimes();
}
