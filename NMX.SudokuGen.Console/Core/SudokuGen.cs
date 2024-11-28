namespace NMX.SudokuGen.Console.Core;
using Library.Core;
using System;

public static class SudokuGen
{
    private const string appName = nameof(SudokuGen), appVersion = "0.1",
        cmdVersion = "version", cmdHelp = "help", cmdCreate = "create", cmdSolve = "solve",
        flgRank = "-r", flgInput = "-i", flgOutput = "-o",
        expRank = "3", expInput = "./inputs.txt", expOutput = "./outputs.txt",
        expPuzzle = "/000000000/23234220423/40000234/23400000/2340000023/",
        expSolution = "/6554675667/2323422234423/4234234/234234/23423423/";
    private static readonly string
        help = @$"
=== {appName} usage info ===

    commands -  
        1. {cmdCreate} {'\t'}: creates sudoku
        2. {cmdSolve}{'\t'}: solves sudoku
        3. {cmdVersion}{'\t'}: shows app version
        4. {cmdHelp}{'\t'}: shows usage info

    options -
        1. {flgRank}{'\t'}: specify rank of sudoku
        2. {flgInput}{'\t'}: specify input .txt file
        3. {flgOutput}{'\t'}: specify output .txt file

    examples -
        1. {cmdCreate} {flgRank} {expRank}
        2. {cmdCreate} {flgInput} {expInput}
        3. {cmdSolve} {expPuzzle}
        4. {cmdSolve} {flgInput} {expInput} {flgOutput} {expOutput}
    
";
    private static readonly string[] commands = [cmdCreate, cmdSolve, cmdVersion, cmdHelp];
    private static readonly Dictionary<string, string?>
        options = new() { { flgRank, null }, { flgInput, null }, { flgOutput, null } };

    private static void ProcessOutput(in string p_output) => Console.WriteLine($"<< {p_output}");

    private static void ProcessInput(in string[] p_inputs)
    {
        if (p_inputs == null) return;
        static void HandleError(in string p_error) => Console.WriteLine($"Error: {p_error}");
        string? a_command = null;
        for (int i = 1, i_v = i + 1; i < p_inputs.Length; ++i, i_v = i + 1)
        {
            if (string.IsNullOrEmpty(p_inputs[i])) continue;
            p_inputs[i] = p_inputs[i].ToLower();
            //-- extract command
            if (commands.Contains(p_inputs[i]))
            {
                if (!string.IsNullOrEmpty(a_command)) { HandleError($"multiple commands"); return; }
                a_command = p_inputs[i];
            }
            //-- extract options
            if (i_v < p_inputs.Length && options.ContainsKey(p_inputs[i]))
            {
                if (options[p_inputs[i]] != null) { HandleError($"duplicate option"); return; }
                options[p_inputs[i]] = p_inputs[i_v];
            }
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
        Console.Write("\n|  Puzz: "); for (int i = 0; i < p_sudoku.puzzle.Length; ++i) Console.Write($"{p_sudoku.puzzle[i]},");
        Console.Write("\n|  Soln: "); for (int i = 0; i < p_sudoku.solution.Length; ++i) Console.Write($"{p_sudoku.solution[i]},");
        Console.WriteLine();
    }
    private static void Test()
    {
        //Sudoku.Create(2, 2 * 2 * 2 * 2).Print();
        Sudoku.Create(3, 3 * 3 * 3 * 3).Print();
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
        //DateTime _old_start = DateTime.Now;
        //new SudokuOperations().GetSudoku(80);
        //DateTime _old_end = DateTime.Now;
        //Console.WriteLine($"OldGen :\t{(_old_end - _old_start).TotalMilliseconds} ms");
        DateTime _new_start = DateTime.Now;
        Sudoku.Create(3, 80);
        DateTime _new_end = DateTime.Now;
        Console.WriteLine($"NewGen :\t{(_new_end - _new_start).TotalMilliseconds} ms");
    }
    private static void TestAcuracy()
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
            Sudoku _sudoku = Sudoku.Create(3, 80);
            //_sudoku.puzzle[0] = 6;s
            Sudoku _sudoku2 = Sudoku.Solve(_sudoku.puzzle);
            _new_pass = Utility.Same(_sudoku.solution, _sudoku2.solution);
        }
        Console.WriteLine($"NewGen :\t{(_new_pass ? "pass" : "fail")}");
    }

    public static void Main(string[] p_args)
        => ProcessInput(p_args);
    //=> Test();
    //=> TestTimes();
    //=> TestAcuracy();
}
