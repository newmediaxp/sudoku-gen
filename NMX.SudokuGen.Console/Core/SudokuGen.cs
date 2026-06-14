// Copyright (c) 2024-2026 New Media XP. Licensed under the MIT License.

namespace NMX.SudokuGen.Console.Core;

using Library.Core;
using CLAP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public static class SudokuGen
{
    private const int dftRank = 3, dftTimes = 1;
    private const string appName = nameof(SudokuGen),
        cmdVersion1 = "version", cmdVersion2 = "--version", cmdVersion3 = "-v",
        cmdHelp1 = "help", cmdHelp2 = "--help", cmdHelp3 = "-h",
        cmdCreate = "create", cmdSolve = "solve", cmdShuffle = "shuffle",
        fgvRank = "-r", fgvBlanks = "-b", fgvCount = "-c", fgvSeed = "-s", fgvInput = "-i", fgvOutput = "-o",
        flgSolutionOutput = "-osol", flgPuzzleOutput = "-opuz", flgMinimalOutput = "-omin", flgBoardOutput = "-oboard", flgExactBlanks = "-exact", flgParallel = "-para",
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
                2. {{fgvBlanks}} {{"\t"}}: Specify target blanks in sudoku. Blanks = { b | b ∈ N, -1 <= b <= r^4 } or max ( max = -1 = r^4 ) where Command = { {{cmdCreate}} }.
                3. {{fgvCount}} {{"\t"}}: Specify number of sudokus. Count = { t | t ∈ N, t >= 1 } where Command = { {{cmdCreate}} }.
                4. {{fgvSeed}} {{"\t"}}: Specify seed for reproducible results. Where Command = { {{cmdCreate}}, {{cmdShuffle}} }.
                5. {{fgvInput}} {{"\t"}}: Specify input .txt file. Where Command = { {{cmdSolve}}, {{cmdShuffle}} }.
                6. {{fgvOutput}} {{"\t"}}: Specify output .txt file. Where Command = { {{cmdCreate}}, {{cmdSolve}}, {{cmdShuffle}} }.

            flags without value -
                1. {{flgPuzzleOutput}} {{"\t"}}: Include the puzzle. Where Command = { {{cmdSolve}} }.
                2. {{flgSolutionOutput}} {{"\t"}}: Include the solution. Where Command = { {{cmdCreate}}, {{cmdShuffle}} }.
                3. {{flgMinimalOutput}} {{"\t"}}: Give minimal output. Where Command = { {{cmdCreate}}, {{cmdSolve}}, {{cmdShuffle}} }.
                4. {{flgBoardOutput}} {{"\t"}}: Draw output in a formatted board. Where Command = { {{cmdCreate}}, {{cmdSolve}}, {{cmdShuffle}} }.
                5. {{flgExactBlanks}} {{"\t"}}: Skip the internal search budget. Can run very long at high ranks. Where Command = { {{cmdCreate}} }.
                6. {{flgParallel}} {{"\t"}}: Spread a batch (Count > 1) across CPU cores. Where Command = { {{cmdCreate}}, {{cmdSolve}}, {{cmdShuffle}} }.

            examples -
                1. {{cmdCreate}}
                2. {{cmdCreate}} {{fgvRank}} {{expRank}} {{fgvBlanks}} {{expBlanks}} {{fgvSeed}} {{expSeed}}
                3. {{cmdCreate}} {{fgvBlanks}} -1 {{flgExactBlanks}}
                4. {{cmdCreate}} {{fgvCount}} {{expTimes}} {{fgvOutput}} {{expOutput}}
                5. {{cmdSolve}} {{expPuzz}}
                6. {{cmdSolve}} {{fgvInput}} {{expInput}} {{fgvOutput}} {{expOutput}}
                7. {{cmdShuffle}} {{expPuzz}} {{fgvSeed}} {{expSeed}}

        """;

    private static string GetSudokuVisual(in Sudoku p_sudoku, in string p_time, in bool p_showRank,
        in bool p_showPuzzle, in bool p_showSolution, in bool p_drawBoard)
    {
        StringBuilder a_visual = new();
        if (p_showRank) _ = a_visual.Append($"\trank={p_sudoku.rank}\tgiven={p_sudoku.squares - p_sudoku.Removed}\ttime={p_time}\n");
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
            if (p_showPuzzle) { a_visual.Append("--puz--\n"); DrawBoard(p_sudoku.Puzzle); }
            if (p_showSolution) { a_visual.Append("--sol--\n"); DrawBoard(p_sudoku.Solution); }
        }
        else
        {
            if (p_showPuzzle) a_visual.Append($"puz=({Utility.GetPuzzleCode(p_sudoku.Puzzle)})");
            if (p_showPuzzle && p_showSolution) a_visual.Append('\n');
            if (p_showSolution) a_visual.Append($"sol=({Utility.GetPuzzleCode(p_sudoku.Solution)})");
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
            [flgMinimalOutput, flgBoardOutput, flgSolutionOutput, flgPuzzleOutput, flgExactBlanks, flgParallel],
            [fgvRank, fgvBlanks, fgvCount, fgvSeed, fgvInput, fgvOutput]);
        (bool a_success, string a_cmd_or_msg) = a_clap.Process(p_inputs);
        if (!a_success) { PrintError($"{a_cmd_or_msg} (try '{cmdHelp1}')"); return 1; }
        int a_rank = dftRank, a_times = dftTimes;
        int? a_blanks = null, a_seed = null;
        bool a_minimalOutput = false, a_boardOutput = false, a_solutionOutput = false, a_puzzleOutput = false, a_exactBlanks = false, a_parallel = false;
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
                case flgPuzzleOutput:
                    if (a_cmd_or_msg is not cmdSolve)
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    a_puzzleOutput = true;
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
                case flgParallel:
                    if (a_cmd_or_msg is not (cmdCreate or cmdSolve or cmdShuffle))
                    { Error_FlagForCommand(a_kvp.Key, a_cmd_or_msg); return 1; }
                    a_parallel = true;
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
                    // -1 (Sudoku.maxBlanks) means "as many as possible"; it flows straight through to Create, with 'max' as a friendly alias
                    if (string.Equals(a_kvp.Value, valBlanksMax, StringComparison.OrdinalIgnoreCase))
                    { a_blanks = Sudoku.maxBlanks; break; }
                    if (!int.TryParse(a_kvp.Value, out int a_blanksValue) || a_blanksValue < Sudoku.maxBlanks)
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
        // blanks default and upper bound depend on the (possibly flag-set) rank, so resolve them last;
        // max (Sudoku.maxBlanks, -1) passes straight through and is resolved inside Create
        int a_squares = a_rank * a_rank * a_rank * a_rank;
        int a_blanksFinal = a_blanks ?? a_rank * a_rank * a_rank;
        if (a_blanksFinal > a_squares) { Error_ValueForFlag(fgvBlanks, $"{a_blanksFinal}"); return 1; }
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
            {
                // each puzzle is an independent job; -par spreads the batch across cores, output stays in order
                string[] a_rendered = new string[a_times];
                bool[] a_failed = new bool[a_times];
                string RenderCreate(int p_i)
                {
                    StringBuilder a_sb = new(a_times > 1 ? $"{1 + p_i}. " : ">> ");
                    try
                    {
                        Stopwatch a_watch = Stopwatch.StartNew();
                        Sudoku a_sudoku = a_seed is int a_seedNum
                            ? Sudoku.Create(a_rank, a_blanksFinal, a_seedNum, a_exactBlanks)
                            : Sudoku.Create(a_rank, a_blanksFinal, a_exactBlanks);
                        a_sb.Append(GetSudokuVisual(a_sudoku, $"{a_watch.Elapsed.TotalMilliseconds}ms",
                            !a_minimalOutput, true, a_solutionOutput, a_boardOutput));
                    }
                    catch (Exception p_ex) { a_sb.Append($"Error: {p_ex.Message}\n"); a_failed[p_i] = true; }
                    return a_sb.ToString();
                }
                Stopwatch a_batch = Stopwatch.StartNew();
                if (a_parallel && a_times > 1) Parallel.For(0, a_times, p_i => a_rendered[p_i] = RenderCreate(p_i));
                else for (int i = 0; i < a_times; ++i) a_rendered[i] = RenderCreate(i);
                a_batch.Stop();
                for (int i = 0; i < a_times; ++i) { a_output.Append(a_rendered[i]); if (a_failed[i]) a_anyFailed = true; }
                // a single wall-clock total — meaningful where the per-puzzle times overlap under -para
                if (!a_minimalOutput && a_times > 1) a_output.Append($">> \tTime={a_batch.Elapsed.TotalMilliseconds}ms\n");
                WriteOutput();
                break;
            }
            case cmdSolve:
            case cmdShuffle:
            {
                List<string>? a_puzzCodes = ReadPuzzleCodes();
                if (a_puzzCodes is null) return 1;
                int a_n = a_puzzCodes.Count;
                string[] a_rendered = new string[a_n];
                bool[] a_failed = new bool[a_n];
                string RenderSolve(int p_i)
                {
                    StringBuilder a_sb = new(a_minimalOutput ? "> " : $"{1 + p_i}.\t");
                    try
                    {
                        Stopwatch a_watch = Stopwatch.StartNew();
                        Sudoku a_sudoku = Sudoku.Solve(Utility.GetPuzzleArr(a_puzzCodes[p_i]));
                        if (a_cmd_or_msg is cmdShuffle)
                            a_sudoku = a_seed is int a_seedNum ? a_sudoku.Shuffle(a_seedNum) : a_sudoku.Shuffle();
                        a_sb.Append(GetSudokuVisual(a_sudoku, $"{a_watch.Elapsed.TotalMilliseconds}ms",
                            !a_minimalOutput, a_cmd_or_msg is cmdShuffle || a_puzzleOutput, a_cmd_or_msg is cmdSolve || a_solutionOutput, a_boardOutput));
                    }
                    catch (Exception p_ex) { a_sb.Append($"Error: {p_ex.Message}\n"); a_failed[p_i] = true; }
                    return a_sb.ToString();
                }
                Stopwatch a_batch = Stopwatch.StartNew();
                if (a_parallel && a_n > 1) Parallel.For(0, a_n, p_i => a_rendered[p_i] = RenderSolve(p_i));
                else for (int i = 0; i < a_n; ++i) a_rendered[i] = RenderSolve(i);
                a_batch.Stop();
                for (int i = 0; i < a_n; ++i) { a_output.Append(a_rendered[i]); if (a_failed[i]) a_anyFailed = true; }
                // a single wall-clock total — meaningful where the per-puzzle times overlap under -para
                if (!a_minimalOutput && a_n > 1) a_output.Append($"total={a_batch.Elapsed.TotalMilliseconds}ms\n");
                WriteOutput();
                break;
            }
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
