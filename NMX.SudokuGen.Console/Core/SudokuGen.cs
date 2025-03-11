namespace NMX.SudokuGen.Console.Core;
using Library.Core;
using CLAP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class SudokuGen
{
    private const string appName = nameof(SudokuGen), appVersion = "0.1.0",
        cmdVersion = "version", cmdHelp = "help", cmdCreate = "create", cmdSolve = "solve",
        fgvRank = "-r", fgvBlanks = "-b", fgvTimes = "-t", fgvInput = "-i", fgvOutput = "-o",
        flgMinimalOutput = "-omin", flgBoardOutput = "-oboard",
        expInput = "./inputs.txt", expOutput = "./outputs.txt",
        expPuzz = "000000000.23234220423.40000234.23400000.2340000023",
        expSoln = "6554675667.2323422234423.4234234.234234.23423423";
    private const int dftRank = 3, dftBlanks = dftRank * dftRank * dftRank, dftTimes = 1;
    private static readonly string
        helpInfo = @$"
=== {appName} - usage info ===

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
        1. {flgBoardOutput}{'\t'}: Draw output in a formatted board. Where Command = {{ {cmdCreate}, {cmdSolve} }}.

    examples -
        1. {cmdCreate}
        1. {cmdCreate} {fgvRank} {dftRank} {fgvBlanks} {dftBlanks}
        2. {cmdCreate} {fgvTimes} {dftTimes} {fgvOutput} {expOutput}
        3. {cmdSolve} {expPuzz}
        4. {cmdSolve} {fgvInput} {expInput} {fgvOutput} {expOutput}

";

    private static string GetSudokuVisual(in Sudoku p_sudoku, in string p_time, in bool p_showRank,
        in bool p_showPuzzle, in bool p_showSolution, in bool p_drawBoard)
    {
        StringBuilder a_visual = new();
        if (p_showRank) a_visual.Append($"rank={p_sudoku.rank}\tgiven={p_sudoku.squares - p_sudoku.Removed}\ttime={p_time}\n");
        if (p_drawBoard)
        {
            int a_rows = p_sudoku.rows, a_squares = p_sudoku.squares;
            void DrawLine()
            {
                a_visual.Append('+');
                for (int i = 0; i < a_rows; ++i) a_visual.Append("---+");
                a_visual.Append('\n');
            }
            void DrawBoard(in int[] p_puzz)
            {
                DrawLine();
                for (int i = 0; i < a_squares; ++i)
                {
                    a_visual.Append($"| {(p_puzz[i] == 0 ? " " : p_puzz[i] > 9 ? 'A' + p_puzz[i] - 1 : p_puzz[i])} ");
                    if (i % a_rows + 1 == a_rows) { a_visual.Append('|').Append('\n'); DrawLine(); }
                }
            }
            if (!p_showRank) a_visual.Append('\n');
            if (p_showPuzzle) DrawBoard(p_sudoku.puzzle);
            if (p_showSolution) DrawBoard(p_sudoku.solution);
        }
        else
        {
            if (p_showPuzzle) a_visual.Append(Utility.GetPuzzleCode(p_sudoku.puzzle));
            if (p_showSolution) a_visual.Append(Utility.GetPuzzleCode(p_sudoku.solution));
            a_visual.Append('\n');
        }
        return a_visual.ToString();
    }
    private static void Print(in string p_message) => Console.WriteLine(p_message);
    private static void ProcessInputs(in string[] p_inputs)
    {
        void Error_FlagForCommand(in string p_flag, in string p_command)
            => Print($"invalid flag '{p_flag}' for command '{p_command}'");
        void Error_ValueForFlag(in string p_flag, in string p_value)
            => Print($"invalid value '{p_value}' for flag '{p_flag}'");
        CLAP a_clap = new([cmdCreate, cmdSolve, cmdVersion, cmdHelp], [flgMinimalOutput, flgBoardOutput],
            [fgvRank, fgvBlanks, fgvTimes, fgvInput, fgvOutput]);
        (bool a_success, string a_cmd_or_msg) = a_clap.Process(p_inputs);
        if (!a_success) { Print(a_cmd_or_msg); return; }
        int a_rank = dftRank, a_blanks = dftBlanks, a_times = dftTimes;
        bool a_minimalOutput = false, a_boardOutput = false;
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
                case flgBoardOutput:
                    if (a_cmd_or_msg is not (cmdCreate or cmdSolve))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return; }
                    a_boardOutput = a_kvp.Value;
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
                        a_output.Append(GetSudokuVisual(a_sudoku, $"{(DateTime.Now - a_start).TotalMilliseconds}ms",
                            !a_minimalOutput, true, !a_minimalOutput, a_boardOutput));
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
                        a_output.Append(a_minimalOutput ? ">" : $"{1 + i}.\t");
                        DateTime a_start = DateTime.Now;
                        Sudoku a_sudoku = Sudoku.Solve(Utility.GetPuzzleArr(a_puzzCodes[i]));
                        a_output.Append(GetSudokuVisual(a_sudoku, $"{(DateTime.Now - a_start).TotalMilliseconds}ms",
                            !a_minimalOutput, !a_minimalOutput, true, a_boardOutput));
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
    private static void TestAcuracy()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 3 * 3 * 3 * 3);
        //_sudoku.puzzle[0] = 6;
        Sudoku a_sudoku2 = Sudoku.Solve(a_sudoku.puzzle);
        Console.WriteLine($"NewGen :\t{(Utility.Same(a_sudoku.solution, a_sudoku2.solution) ? "pass" : "fail")}");
    }

    public static void Main(string[] p_args) => ProcessInputs(p_args);
}
