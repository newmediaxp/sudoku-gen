namespace NMX.SudokuGen.Library.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class Utility
    {
        public static readonly Random random = new Random();
        public static void Copy(in int[] p_from, in int[] p_to) => Array.Copy(p_from, 0, p_to, 0, p_to.Length);
        public static bool Same(in int[] p_arr1, in int[] p_arr2)
        { for (int i = 0; i < p_arr1.Length; ++i) if (p_arr1[i] != p_arr2[i]) return false; return true; }
        public static int Count(in int[] p_arr, in int p_input)
        { int a_count = 0; for (int i = 0; i < p_arr.Length; ++i) if (p_arr[i] == p_input) ++a_count; return a_count; }
        public static void Swap2(ref int p_num1, ref int p_num2)
        { int a_temp = p_num1; p_num1 = p_num2; p_num2 = a_temp; }
        public static void Swap3(ref int p_num1, ref int p_num2, ref int p_num3)
        { int a_temp = p_num1; p_num1 = p_num2; p_num2 = p_num3; p_num3 = a_temp; }
        public static void Init(in int[] p_arr) { for (int i = 0; i <= p_arr.Length; ++i) p_arr[i] = 0; }
        public static void InitRandom(in int[] p_arr, in int p_length, in bool p_plus1)
        {
            for (int i = 0; i < p_arr.Length; ++i) p_arr[i] = i % p_length + (p_plus1 ? 1 : 0);
            for (int r, i = 0; i < p_arr.Length; ++i)
            {
                int a_rows = i / p_length, a_start = a_rows * p_length, a_nextStart = (a_rows + 1) * p_length;
                r = random.Next(a_start, a_nextStart); Swap2(ref p_arr[i], ref p_arr[r]);
            }
        }
        public static string GetPuzzleCode(in int[] p_puzz)
        {
            int a_rows = (int)Math.Sqrt(p_puzz.Length);
            StringBuilder a_puzzleCode = new StringBuilder(p_puzz.Length + a_rows);
            for (int i = 0; i < p_puzz.Length; ++i)
            {
                if (i > 0 && i % a_rows == 0) a_puzzleCode.Append('.');
                a_puzzleCode.Append(p_puzz[i]);
            }
            return a_puzzleCode.ToString();
        }
        public static int[] GetPuzzleArr(in string p_code)
        {
            int a_num;
            List<int> a_puzzle = new List<int>(p_code.Length);
            foreach (char c in p_code) if (char.IsDigit(c) && (a_num = c - '0') >= 0) a_puzzle.Add(a_num);
            return a_puzzle.ToArray();
        }
    }
}
