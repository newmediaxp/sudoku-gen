namespace NMX.SudokuGen.Console.Core;
using Library.Core;
using CLAP;
using System;
using System.Collections.Generic;

public static class SudokuGen
{
    private const string appName = nameof(SudokuGen), appVersion = "0.1",
        cmdVersion = "version", cmdHelp = "help", cmdCreate = "create", cmdSolve = "solve",
        fgvRank = "-r", fgvBlanks = "-b", fgvInput = "-i", fgvOutput = "-o",
        expInput = "./inputs.txt", expOutput = "./outputs.txt",
        expPuzzle = "/000000000/23234220423/40000234/23400000/2340000023/",
        expSolution = "/6554675667/2323422234423/4234234/234234/23423423/";
    private const int dftRank = 3, dftBlank = dftRank * dftRank * dftRank;
    private static readonly string
        help = @$"
=== {appName} usage info ===

    commands -  
        1. {cmdCreate} {'\t'}: creates sudoku
        2. {cmdSolve}{'\t'}: solves sudoku
        3. {cmdVersion}{'\t'}: shows app version
        4. {cmdHelp}{'\t'}: shows usage info

    flags with value -
        1. {fgvRank}{'\t'}: specify rank of sudoku
        2. {fgvBlanks}{'\t'}: specify number of blanks in sudoku
        2. {fgvInput}{'\t'}: specify input .txt file
        3. {fgvOutput}{'\t'}: specify output .txt file

    examples -
        1. {cmdCreate}
        1. {cmdCreate} {fgvRank} {dftRank} {fgvBlanks} {dftBlank}
        2. {cmdCreate} {fgvOutput} {expOutput}
        3. {cmdSolve} {expPuzzle}
        4. {cmdSolve} {fgvInput} {expInput} {fgvOutput} {expOutput}

";

    private static void PrintError(in string p_message) => Console.WriteLine($"Error: {p_message}");

    private static void ProcessInputs(in string[] p_inputs)
    {
        CLAP a_clap = new([cmdCreate, cmdSolve, cmdVersion, cmdHelp], [], [fgvRank, fgvBlanks, fgvInput, fgvOutput]);
        (bool a_success, string a_message) = a_clap.ProcessInputs(p_inputs);
        if (!a_success) { PrintError(a_message); return; }
        int a_rank = dftRank, a_blank = dftBlank;
        bool a_input, a_output;
        foreach (KeyValuePair<string, string?> a_kvp in a_clap.flagsWithValue)
        {
            if (a_kvp.Value is null) continue;
            switch (a_kvp.Key)
            {
                case fgvRank:
                    if (!int.TryParse(a_kvp.Value, out a_rank) || a_rank < 1 || a_rank > 4)
                    { PrintError($"invalid rank '{a_kvp.Value}'"); return; }
                    break;
                case fgvBlanks:
                    if (!int.TryParse(a_kvp.Value, out a_blank) || a_blank < 1)
                    { PrintError($"invalid blanks '{a_kvp.Value}'"); return; }
                    break;
                case fgvInput:
                    if (!a_kvp.Value.EndsWith(".txt")) a_input = true;
                    break;
                case fgvOutput:
                    if (!a_kvp.Value.EndsWith(".txt")) a_output = true;
                    break;
                default: PrintError($"invalid flag '{a_kvp.Key}'"); return;
            }
        }
        switch (a_message)
        {
            case cmdCreate:

                break;
            case cmdSolve:
                break;
            case cmdVersion:
                break;
            case cmdHelp:
                break;
            default: PrintError($"invalid command '{a_message}'"); return;
        }
    }

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
        Console.Write("\n|  Puzz: "); for (int i = 0; i < p_sudoku.puzzle.Length; ++i) Console.Write($"{p_sudoku.puzzle[i]}");
        Console.Write("\n|  Soln: "); for (int i = 0; i < p_sudoku.solution.Length; ++i) Console.Write($"{p_sudoku.solution[i]}");
        Console.WriteLine();
    }
    private static async void Test()
    {
        //Sudoku.Create(2, 2 * 2 * 2 * 2).Print();
        (await Sudoku.Create(3, 3 * 3 * 3 * 3)).Print();
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
    private static async void TestTimes()
    {
        //DateTime _old_start = DateTime.Now;
        //new SudokuOperations().GetSudoku(80);
        //DateTime _old_end = DateTime.Now;
        //Console.WriteLine($"OldGen :\t{(_old_end - _old_start).TotalMilliseconds} ms");
        DateTime _new_start = DateTime.Now;
        await Sudoku.Create(3, 80);
        DateTime _new_end = DateTime.Now;
        Console.WriteLine($"NewGen :\t{(_new_end - _new_start).TotalMilliseconds} ms");
    }
    private static async void TestAcuracy()
    {
        //bool _old_pass;
        //{
        //    SudokuOperations _sop = new();
        //    int[] _all = _sop.GetSudoku(80);
        //    int[] _puzz = new int[_all.Length]; int[] _sol = new int[_all.Length];
        //    for (int i = 0; i < _all.Length; ++i) { _puzz[i] = _all[i] % 10; _sol[i] = _all[i] / 10; }
        //    int[] _sol2 = _sop.SolveSudoku(_puzz);
        //    _old_pass = Utility.Same(_sol, _sol2);
        //}
        //Console.WriteLine($"OldGen :\t{(_old_pass ? "pass" : "fail")}");
        bool _new_pass;
        {
            Sudoku _sudoku = await Sudoku.Create(3, 80);
            //_sudoku.puzzle[0] = 6;s
            Sudoku _sudoku2 = await Sudoku.Solve(_sudoku.puzzle);
            _new_pass = Utility.Same(_sudoku.solution, _sudoku2.solution);
        }
        Console.WriteLine($"NewGen :\t{(_new_pass ? "pass" : "fail")}");
    }

    public static void Main(string[] p_args)
        //=> ProcessInputs(p_args);
    => Test();
    //=> TestTimes();
    //=> TestAcuracy();
}