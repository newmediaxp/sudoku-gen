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
        expPuzz = "000000000.23234220423.40000234.23400000.2340000023",
        expSoln = "6554675667.2323422234423.4234234.234234.23423423";
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
        3. {cmdSolve} {expPuzz}
        4. {cmdSolve} {fgvInput} {expInput} {fgvOutput} {expOutput}

";

    private static void PrintError(in string p_message) => Console.WriteLine($"Error: {p_message}");

    private static void ProcessInputs(in string[] p_inputs)
    {
        CLAP a_clap = new([cmdCreate, cmdSolve, cmdVersion, cmdHelp], [], [fgvRank, fgvBlanks, fgvInput, fgvOutput]);
        (bool a_success, string a_cmdORmsg) = a_clap.ProcessInputs(p_inputs);
        if (!a_success) { PrintError(a_cmdORmsg); return; }
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
                    if (!a_kvp.Value.EndsWith(".txt"))
                    { PrintError($"invalid input path '{a_kvp.Value}'"); return; }
                    a_input = true;
                    break;
                case fgvOutput:
                    if (!a_kvp.Value.EndsWith(".txt"))
                    { PrintError($"invalid output path '{a_kvp.Value}'"); return; }
                    a_output = true;
                    break;
                default: PrintError($"invalid flag '{a_kvp.Key}'"); return;
            }
        }
        switch (a_cmdORmsg)
        {
            case cmdCreate:

                break;
            case cmdSolve:
                break;
            case cmdVersion:
                break;
            case cmdHelp:
                break;
            default: PrintError($"invalid command '{a_cmdORmsg}'"); return;
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
    private static void TestCreation()
    {
        //Sudoku.Create(2, 2 * 2 * 2 * 2).Print();
        (Sudoku.Create(3, 3 * 3 * 3 * 3)).Print();
    }
    private static void TestTimes(in int p_times)
    {
        DateTime a_start = DateTime.Now;
        for (int i = 0; i < p_times; ++i) Sudoku.Create(3, 3 * 3 * 3 * 3);
        Console.WriteLine($"NewGen :\t{(DateTime.Now - a_start).TotalMilliseconds/p_times} ms");
    }
    private static void TestAcuracy()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 80);
        //_sudoku.puzzle[0] = 6;
        Sudoku a_sudoku2 = Sudoku.Solve(a_sudoku.puzzle);
        Console.WriteLine($"NewGen :\t{(Utility.Same(a_sudoku.solution, a_sudoku2.solution) ? "pass" : "fail")}");
    }

    public static void Main(string[] p_args)
    //=> ProcessInputs(p_args);
    //=> TestCreation();
    => TestTimes(5);
    //=> TestAcuracy();
}
