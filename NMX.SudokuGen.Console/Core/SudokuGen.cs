namespace NMX.SudokuGen.Console.Core;

using Library.Core;
using CLAP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

public static class SudokuGen
{
    private const int dftRank = 3, dftTimes = 1;
    private const string appName = nameof(SudokuGen),
        cmdVersion1 = "version", cmdVersion2 = "--version", cmdVersion3 = "-v",
        cmdHelp1 = "help", cmdHelp2 = "--help", cmdHelp3 = "-h",
        cmdCreate = "create", cmdSolve = "solve", cmdShuffle = "shuffle",
        fgvRank = "-r", fgvBlanks = "-b", fgvCount = "-c", fgvSeed = "-s", fgvInput = "-i", fgvOutput = "-o",
        flgSolutionOutput = "-osol", flgMinimalOutput = "-omin", flgBoardOutput = "-oboard", flgExactBlanks = "-exact",
        valBlanksMax = "max", valMinRank = "2", valMaxRank = "5",
        expRank = "4", expBlanks = "120", expTimes = "10", expSeed = "777",
        expInput = "./inputs.txt", expOutput = "./outputs.txt",
        expPuzz = "390560027.001809405.600172893.180257900.500643000.204081570.003096052.050328619.962010300";

    private const string
        helpInfo = $$"""

        === {{appName}} - usage info ===

            commands -
                1. {{cmdCreate}} {{"\t\t\t"}}: Creates sudoku. Default rank = 3 and blanks = rank^3.
                2. {{cmdSolve}} {{"\t\t\t"}}: Solves sudoku.
                3. {{cmdShuffle}} {{"\t\t\t"}}: Shuffles sudoku into an equivalent variant.
                4. {{cmdVersion1}}, {{cmdVersion2}}, {{cmdVersion3}} {{"\t"}}: Shows app version.
                5. {{cmdHelp1}}, {{cmdHelp2}}, {{cmdHelp3}} {{"\t\t"}}: Shows usage info.

            flags with value -
                1. {{fgvRank}} {{"\t"}}: Specify rank of sudoku. Rank = { r | r ∈ N, {{valMinRank}} <= r <= {{valMaxRank}} } where Command = { {{cmdCreate}} }.
                2. {{fgvBlanks}} {{"\t"}}: Specify target blanks in sudoku. Blanks = { b | b ∈ N, 0 <= b <= Rank^4 } or '{{valBlanksMax}}' where Command = { {{cmdCreate}} }.
                3. {{fgvCount}} {{"\t"}}: Specify number of sudokus. Count = { t | t ∈ N, t >= 1 } where Command = { {{cmdCreate}} }.
                4. {{fgvSeed}} {{"\t"}}: Specify seed for reproducible results. Where Command = { {{cmdCreate}}, {{cmdShuffle}} }.
                5. {{fgvInput}} {{"\t"}}: Specify input .txt file. Where Command = { {{cmdSolve}}, {{cmdShuffle}} }.
                6. {{fgvOutput}} {{"\t"}}: Specify output .txt file. Where Command = { {{cmdCreate}}, {{cmdSolve}}, {{cmdShuffle}} }.

            flags without value -
                1. {{flgSolutionOutput}} {{"\t"}}: Include the solution. Where Command = { {{cmdCreate}}, {{cmdShuffle}} }.
                2. {{flgMinimalOutput}} {{"\t"}}: Give minimal output. Where Command = { {{cmdCreate}}, {{cmdSolve}}, {{cmdShuffle}} }.
                3. {{flgBoardOutput}} {{"\t"}}: Draw output in a formatted board. Where Command = { {{cmdCreate}}, {{cmdSolve}}, {{cmdShuffle}} }.
                4. {{flgExactBlanks}} {{"\t"}}: Skip the internal search budget. Can run very long at high ranks. Where Command = { {{cmdCreate}} }.

            examples -
                1. {{cmdCreate}}
                2. {{cmdCreate}} {{fgvRank}} {{expRank}} {{fgvBlanks}} {{expBlanks}} {{fgvSeed}} {{expSeed}}
                3. {{cmdCreate}} {{fgvBlanks}} {{valBlanksMax}} {{flgExactBlanks}}
                4. {{cmdCreate}} {{fgvCount}} {{expTimes}} {{fgvOutput}} {{expOutput}}
                5. {{cmdSolve}} {{expPuzz}}
                6. {{cmdSolve}} {{fgvInput}} {{expInput}} {{fgvOutput}} {{expOutput}}
                7. {{cmdShuffle}} {{expPuzz}} {{fgvSeed}} {{expSeed}}

        """;

    private static string GetSudokuVisual(in Sudoku p_sudoku, in string p_time, in bool p_showRank,
        in bool p_showPuzzle, in bool p_showSolution, in bool p_drawBoard)
    {
        StringBuilder a_visual = new();
        if (p_showRank) _ = a_visual.Append($"rank={p_sudoku.rank}\tgiven={p_sudoku.squares - p_sudoku.Removed}\ttime={p_time}\n");
        if (p_drawBoard)
        {
            int a_rows = p_sudoku.rows, a_squares = p_sudoku.squares;
            void DrawLine()
            {
                a_visual.Append('+');
                for (int i = 0; i < a_rows; ++i) _ = a_visual.Append("---+");
                a_visual.Append('\n');
            }
            void DrawBoard(in IReadOnlyList<int> p_puzz)
            {
                DrawLine();
                for (int i = 0; i < a_squares; ++i)
                {
                    _ = a_visual.Append($"| {(p_puzz[i] == 0 ? ' ' : p_puzz[i] > 9 ? (char)('A' + p_puzz[i] - 10) : (char)('0' + p_puzz[i]))} ");
                    if (i % a_rows + 1 == a_rows) { _ = a_visual.Append('|').Append('\n'); DrawLine(); }
                }
            }
            if (!p_showRank) a_visual.Append('\n');
            if (p_showPuzzle) { a_visual.Append("--puzz--\n"); DrawBoard(p_sudoku.Puzzle); }
            if (p_showSolution) { a_visual.Append("--soln--\n"); DrawBoard(p_sudoku.Solution); }
        }
        else
        {
            if (p_showPuzzle) a_visual.Append($"puzz=({Utility.GetPuzzleCode(p_sudoku.Puzzle)})");
            if (p_showPuzzle && p_showSolution) a_visual.Append('\n');
            if (p_showSolution) a_visual.Append($"soln=({Utility.GetPuzzleCode(p_sudoku.Solution)})");
            a_visual.Append('\n');
        }
        return a_visual.ToString();
    }

    private static int ProcessInputs(in string[] p_inputs)
    {
        // a bare launch is someone exploring, not a script gone wrong - greet with the usage info
        if (p_inputs.Length == 0) { Print(helpInfo); return 0; }
        void Error_FlagForCommand(in string p_flag, in string p_command)
            => PrintError($"invalid flag '{p_flag}' for command '{p_command}'");
        void Error_ValueForFlag(in string p_flag, in string p_value)
            => PrintError($"invalid value '{p_value}' for flag '{p_flag}'");
        CLAP a_clap = new([cmdCreate, cmdSolve, cmdShuffle, cmdVersion1, cmdVersion2, cmdVersion3, cmdHelp1, cmdHelp2, cmdHelp3],
            [flgMinimalOutput, flgBoardOutput, flgSolutionOutput, flgExactBlanks],
            [fgvRank, fgvBlanks, fgvCount, fgvSeed, fgvInput, fgvOutput]);
        (bool a_success, string a_cmd_or_msg) = a_clap.Process(p_inputs);
        if (!a_success) { PrintError($"{a_cmd_or_msg} (try '{cmdHelp1}')"); return 1; }
        int a_rank = dftRank, a_times = dftTimes;
        int? a_blanks = null, a_seed = null;
        bool a_blanksMax = false;
        bool a_minimalOutput = false, a_boardOutput = false, a_solutionOutput = false, a_exactBlanks = false;
        string? a_inputPath = null, a_outputPath = null;
        foreach (KeyValuePair<string, bool> a_kvp in a_clap.flags)
        {
            if (!a_kvp.Value) continue;
            switch (a_kvp.Key)
            {
                case flgSolutionOutput:
                    if (a_cmd_or_msg is not (cmdCreate or cmdShuffle))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    a_solutionOutput = true;
                    break;
                case flgMinimalOutput:
                    if (a_cmd_or_msg is not (cmdCreate or cmdSolve or cmdShuffle))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    a_minimalOutput = true;
                    break;
                case flgBoardOutput:
                    if (a_cmd_or_msg is not (cmdCreate or cmdSolve or cmdShuffle))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    a_boardOutput = true;
                    break;
                case flgExactBlanks:
                    if (a_cmd_or_msg is not cmdCreate)
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    a_exactBlanks = true;
                    break;
                default: PrintError($"unhandled flag '{a_kvp.Key}'"); return 1;
            }
        }
        foreach (KeyValuePair<string, string?> a_kvp in a_clap.flagsWithValue)
        {
            if (a_kvp.Value is null) continue;
            switch (a_kvp.Key)
            {
                case fgvRank:
                    if (a_cmd_or_msg is not cmdCreate)
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    if (!int.TryParse(a_kvp.Value, out a_rank) || a_rank < Sudoku.minRank || a_rank > Sudoku.maxRank)
                    { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return 1; }
                    break;
                case fgvBlanks:
                    if (a_cmd_or_msg is not cmdCreate)
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    if (string.Equals(a_kvp.Value, valBlanksMax, StringComparison.OrdinalIgnoreCase))
                    { a_blanksMax = true; break; }
                    if (!int.TryParse(a_kvp.Value, out int a_blanksValue) || a_blanksValue < 0)
                    { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return 1; }
                    a_blanks = a_blanksValue;
                    break;
                case fgvCount:
                    if (a_cmd_or_msg is not cmdCreate)
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    if (!int.TryParse(a_kvp.Value, out a_times) || a_times < 1)
                    { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return 1; }
                    break;
                case fgvSeed:
                    if (a_cmd_or_msg is not (cmdCreate or cmdShuffle))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    if (!int.TryParse(a_kvp.Value, out int a_seedValue))
                    { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return 1; }
                    a_seed = a_seedValue;
                    break;
                case fgvInput:
                    if (a_cmd_or_msg is not (cmdSolve or cmdShuffle))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    if (!File.Exists(a_kvp.Value))
                    { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return 1; }
                    a_inputPath = a_kvp.Value;
                    break;
                case fgvOutput:
                    if (a_cmd_or_msg is not (cmdCreate or cmdSolve or cmdShuffle))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    try { File.AppendAllText(a_kvp.Value, string.Empty); }
                    catch { Error_ValueForFlag(a_kvp.Key, a_kvp.Value); return 1; }
                    a_outputPath = a_kvp.Value;
                    break;
                default: PrintError($"unhandled flag '{a_kvp.Key}'"); return 1;
            }
        }
        // blanks bound, default and 'max' depend on the (possibly flag-set) rank, so resolve them last
        int a_squares = a_rank * a_rank * a_rank * a_rank;
        if (a_blanks > a_squares) { Error_ValueForFlag(fgvBlanks, $"{a_blanks}"); return 1; }
        int a_blanksFinal = a_blanksMax ? a_squares : a_blanks ?? a_rank * a_rank * a_rank;
        bool a_anyFailed = false;
        StringBuilder a_output = new(a_minimalOutput ? string.Empty : $"[ {DateTime.Now} ]\n");
        void WriteOutput()
        {
            if (!string.IsNullOrEmpty(a_outputPath))
            {
                File.AppendAllText(a_outputPath!, a_output.ToString());
                Print($"output written to {a_outputPath}");
            }
            else Print(a_output.ToString());
        }
        List<string>? ReadPuzzleCodes()
        {
            List<string> a_codes = [.. a_clap.values];
            if (!string.IsNullOrEmpty(a_inputPath)) a_codes.AddRange(File.ReadAllLines(a_inputPath!));
            if (a_codes.Count != 0) return a_codes;
            PrintError($"no puzzle codes given for command '{a_cmd_or_msg}'"); return null;
        }
        switch (a_cmd_or_msg)
        {
            case cmdCreate:
                for (int i = 0; i < a_times; ++i)
                {
                    try
                    {
                        _ = a_output.Append(a_minimalOutput ? "> " : $"{1 + i}.\t");
                        Stopwatch a_watch = Stopwatch.StartNew();
                        Sudoku a_sudoku = a_seed is int a_seedNum
                            ? Sudoku.Create(a_rank, a_blanksFinal, a_seedNum, a_exactBlanks)
                            : Sudoku.Create(a_rank, a_blanksFinal, a_exactBlanks);
                        _ = a_output.Append(GetSudokuVisual(a_sudoku, $"{a_watch.Elapsed.TotalMilliseconds}ms",
                            !a_minimalOutput, true, a_solutionOutput, a_boardOutput));
                    }
                    catch (Exception p_ex) { _ = a_output.Append($"Error: {p_ex.Message}\n"); a_anyFailed = true; }
                }
                WriteOutput();
                break;
            case cmdSolve:
            case cmdShuffle:
                List<string>? a_puzzCodes = ReadPuzzleCodes();
                if (a_puzzCodes is null) return 1;
                for (int i = 0; i < a_puzzCodes.Count; ++i)
                {
                    try
                    {
                        _ = a_output.Append(a_minimalOutput ? "> " : $"{1 + i}.\t");
                        Stopwatch a_watch = Stopwatch.StartNew();
                        Sudoku a_sudoku = Sudoku.Solve(Utility.GetPuzzleArr(a_puzzCodes[i]));
                        if (a_cmd_or_msg is cmdShuffle)
                            a_sudoku = a_seed is int a_seedNum ? a_sudoku.Shuffle(a_seedNum) : a_sudoku.Shuffle();
                        _ = a_output.Append(GetSudokuVisual(a_sudoku, $"{a_watch.Elapsed.TotalMilliseconds}ms",
                            !a_minimalOutput, a_cmd_or_msg is cmdShuffle, a_cmd_or_msg is cmdSolve || a_solutionOutput, a_boardOutput));
                    }
                    catch (Exception p_ex) { _ = a_output.Append($"Error: {p_ex.Message}\n"); a_anyFailed = true; }
                }
                WriteOutput();
                break;
            case cmdVersion1:
            case cmdVersion2:
            case cmdVersion3:
                Version? a_version = typeof(SudokuGen).Assembly.GetName().Version;
                Print($"v{a_version?.ToString(3) ?? "unknown"}");
                break;
            case cmdHelp1:
            case cmdHelp2:
            case cmdHelp3:
                Print(helpInfo);
                break;
            default: PrintError($"unhandled command '{a_cmd_or_msg}'"); return 1;
        }
        return a_anyFailed ? 1 : 0;
    }

    private static void Print(in string p_message) => Console.WriteLine(p_message);
    private static void PrintError(in string p_message) => Console.Error.WriteLine(p_message);
    public static int Main(string[] p_args) => ProcessInputs(p_args);
}
