namespace NMX.SudokuGen.Console.Core;
using Library.Core;
using CLAP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class SudokuGen
{
    private const string appName = nameof(SudokuGen), appVersion = "0.1",
        cmdVersion = "version", cmdHelp = "help", cmdCreate = "create", cmdSolve = "solve",
        fgvRank = "-r", fgvBlanks = "-b", fgvTimes = "-t", fgvInput = "-i", fgvOutput = "-o",
        flgMinimalOutput = "-omin",
        expInput = "./inputs.txt", expOutput = "./outputs.txt",
        expPuzz = "000000000.23234220423.40000234.23400000.2340000023",
        expSoln = "6554675667.2323422234423.4234234.234234.23423423";
    private const int dftRank = 3, dftBlanks = dftRank * dftRank * dftRank, dftTimes = 1;
    private static readonly string
        helpInfo = @$"
=== {appName} usage info ===

    commands -  
        1. {cmdCreate}{'\t'}: Creates sudoku.
        2. {cmdSolve}{'\t'}: Solves sudoku.
        3. {cmdVersion}{'\t'}: Shows app version.
        4. {cmdHelp}{"\t\t"}: Shows usage info.

    flags with value -
        1. {fgvRank}{'\t'}: Specify rank of sudoku. Rank = {{ 2, 3 }} where Command = {{ {cmdCreate} }}.
        2. {fgvBlanks}{'\t'}: Specify desired blanks in sudoku. Blanks = {{ b | b ∈ N, 1 < b < Rank }} where Command = {{ {cmdCreate} }}.
        3. {fgvTimes}{'\t'}: Specify number of sudokus. Times = {{ t | t ∈ N, t > 1 }} where Command = {{ {cmdCreate} }}.
        4. {fgvInput}{'\t'}: Specify input .txt file. Where Command = {{ {cmdSolve} }}.
        5. {fgvOutput}{'\t'}: Specify output .txt file. Where Command = {{ {cmdCreate}, {cmdSolve} }}.

    flags without value -
        1. {flgMinimalOutput}{'\t'}: Give minimal output. Where Command = {{ {cmdCreate}, {cmdSolve} }}.

    examples -
        1. {cmdCreate}
        1. {cmdCreate} {fgvRank} {dftRank} {fgvBlanks} {dftBlanks}
        2. {cmdCreate} {fgvTimes} {dftTimes} {fgvOutput} {expOutput}
        3. {cmdSolve} {expPuzz}
        4. {cmdSolve} {fgvInput} {expInput} {fgvOutput} {expOutput}

";

    private static string GetFormatedSudoku(in Sudoku p_sudoku, in string p_time)
        => $"rank={p_sudoku.rank}\tblanks={p_sudoku.Removed}\ttime={p_time}\n"
        + $"{Utility.GetPuzzleCode(p_sudoku.puzzle)}\n{Utility.GetPuzzleCode(p_sudoku.solution)}\n";
    private static void Print(in string p_message) => Console.WriteLine(p_message);
    private static void ProcessInputs(in string[] p_inputs)
    {
        void Error_FlagForCommand(in string p_flag, in string p_command)
            => Print($"invalid flag '{p_flag}' for command '{p_command}'");
        void Error_ValueForFlag(in string p_flag, in string p_value)
            => Print($"invalid value '{p_value}' for flag '{p_flag}'");
        CLAP a_clap = new([cmdCreate, cmdSolve, cmdVersion, cmdHelp], [flgMinimalOutput], [fgvRank, fgvBlanks, fgvTimes, fgvInput, fgvOutput]);
        (bool a_success, string a_cmd_or_msg) = a_clap.Process(p_inputs);
        if (!a_success) { Print(a_cmd_or_msg); return; }
        int a_rank = dftRank, a_blanks = dftBlanks, a_times = dftTimes;
        bool a_minimalOutput = false;
        string? a_inputPath = null, a_outputPath = null;
        foreach (KeyValuePair<string, bool> a_kvp in a_clap.flags)
        {
            switch (a_kvp.Key)
            {
                case flgMinimalOutput:
                    if (a_cmd_or_msg is not (cmdCreate or cmdSolve))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return; }
                    a_minimalOutput = a_kvp.Value;
                    break;
                default: Print($"unhandled flag '{a_kvp.Key}'"); return;
            }
        }
        foreach (KeyValuePair<string, string?> a_kvp in a_clap.flagsWithValue)
        {
            if (a_kvp.Value is null) continue;
            switch (a_kvp.Key)
            {
                case fgvRank:
                    if (a_cmd_or_msg is not cmdCreate)
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return; }
                    if (!int.TryParse(a_kvp.Value, out a_rank) || a_rank < 2 || a_rank > 3)
                    { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return; }
                    break;
                case fgvBlanks:
                    if (a_cmd_or_msg is not cmdCreate)
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return; }
                    if (!int.TryParse(a_kvp.Value, out a_blanks) || a_blanks < 1)
                    { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return; }
                    break;
                case fgvTimes:
                    if (a_cmd_or_msg is not cmdCreate)
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return; }
                    if (!int.TryParse(a_kvp.Value, out a_times) || a_times < 1)
                    { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return; }
                    break;
                case fgvInput:
                    if (a_cmd_or_msg is not cmdSolve)
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return; }
                    if (!File.Exists(a_kvp.Value))
                    { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return; }
                    a_inputPath = a_kvp.Value;
                    break;
                case fgvOutput:
                    if (a_cmd_or_msg is not (cmdCreate or cmdSolve))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return; }
                    try { File.AppendAllText(a_kvp.Value, string.Empty); }
                    catch { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return; }
                    a_outputPath = a_kvp.Value;
                    break;
                default: Print($"unhandled flag '{a_kvp.Key}'"); return;
            }
        }
        StringBuilder a_output = new(a_minimalOutput ? string.Empty : $"[ {DateTime.Now} ]\n");
        switch (a_cmd_or_msg)
        {
            case cmdCreate:
                for (int i = 0; i < a_times; ++i)
                {
                    try
                    {
                        a_output.Append(a_minimalOutput ? ">" : $"{1 + i}.\t");
                        DateTime a_start = DateTime.Now;
                        Sudoku a_sudoku = Sudoku.Create(a_rank, a_blanks);
                        a_output.Append(a_minimalOutput ? $"{Utility.GetPuzzleCode(a_sudoku.puzzle)}\n"
                            : GetFormatedSudoku(a_sudoku, $"{(DateTime.Now - a_start).TotalMilliseconds}ms"));
                    }
                    catch (Exception p_ex) { a_output.Append($"Error: {p_ex.Message}\n"); }
                }
                if (!string.IsNullOrEmpty(a_outputPath))
                {
                    File.AppendAllText(a_outputPath!, a_output.ToString());
                    Print($"output written to {a_outputPath}");
                }
                else Print(a_output.ToString());
                break;
            case cmdSolve:
                List<string> a_puzzCodes = [.. a_clap.values];
                if (!string.IsNullOrEmpty(a_inputPath)) a_puzzCodes.AddRange(File.ReadAllLines(a_inputPath!));
                for (int i = 0; i < a_puzzCodes.Count; ++i)
                {
                    try
                    {
                        a_output.Append(a_minimalOutput ? "=" : $"{1 + i}.\t");
                        DateTime a_start = DateTime.Now;
                        Sudoku a_sudoku = Sudoku.Solve(Utility.GetPuzzleArr(a_puzzCodes[i]));
                        a_output.Append(a_minimalOutput ? $"{Utility.GetPuzzleCode(a_sudoku.solution)}\n"
                            : GetFormatedSudoku(a_sudoku, $"{(DateTime.Now - a_start).TotalMilliseconds}ms"));
                    }
                    catch (Exception p_ex) { a_output.Append($"Error: {p_ex.Message}\n"); }
                }
                if (!string.IsNullOrEmpty(a_outputPath))
                {
                    File.AppendAllText(a_outputPath!, a_output.ToString());
                    Print($"output written to {a_outputPath}");
                }
                else Print(a_output.ToString());
                break;
            case cmdVersion:
                Print($"v{appVersion}");
                break;
            case cmdHelp:
                Print(helpInfo);
                break;
            default: Print($"unhandled command '{a_cmd_or_msg}'"); return;
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
    private static void TestCreation()
    {
        //Print(GetFormatedSudoku(Sudoku.Create(2, 2 * 2 * 2 * 2), "N/A"));
        Print(GetFormatedSudoku(Sudoku.Create(3, 3 * 3 * 3 * 3), "N/A"));
        //Print(GetFormatedSudoku(Sudoku.Create(4, 4 * 4 * 4 * 2), "N/A"));
    }
    private static void TestTimes(in int p_times)
    {
        DateTime a_start = DateTime.Now;
        for (int i = 0; i < p_times; ++i) Sudoku.Create(3, 3 * 3 * 3 * 3);
        Console.WriteLine($"NewGen :\t{(DateTime.Now - a_start).TotalMilliseconds / p_times:0.00} ms");
    }
    private static void TestAcuracy()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 3 * 3 * 3 * 3);
        //_sudoku.puzzle[0] = 6;
        Sudoku a_sudoku2 = Sudoku.Solve(a_sudoku.puzzle);
        Console.WriteLine($"NewGen :\t{(Utility.Same(a_sudoku.solution, a_sudoku2.solution) ? "pass" : "fail")}");
    }

    public static void Main(string[] p_args) => ProcessInputs(p_args);
}
