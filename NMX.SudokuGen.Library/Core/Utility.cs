namespace NMX.SudokuGen.Library.Core
{
    using System;
    using System.Collections.Generic;

    public static class Utility
    {
        public static readonly Random random = new Random();
        public static void Copy(in int[] p_from, in int[] p_to) => Array.Copy(p_from, 0, p_to, 0, p_to.Length);
        public static bool Same(in int[] p_arr1, in int[] p_arr2)
        { for (int i = 0; i < p_arr1.Length; ++i) if (p_arr1[i] != p_arr2[i]) return false; return true; }
        public static int Count(in int[] p_arr, in int p_input)
        { int a_count = 0; for (int i = 0; i < p_arr.Length; ++i) if (p_arr[i] == p_input) ++a_count; return a_count; }
        public static void Swap3(in int[] p_arr, in int p_i1, in int p_i2, in int p_i3)
        { int a_temp = p_arr[p_i1]; p_arr[p_i1] = p_arr[p_i2]; p_arr[p_i2] = p_arr[p_i3]; p_arr[p_i3] = a_temp; }
        public static void Init(in int[] p_arr) { for (int i = 0; i <= p_arr.Length; ++i) p_arr[i] = 0; }
        public static void Init(in List<int> p_list, in bool p_plus1)
        { p_list.Clear(); for (int i = 0; i < p_list.Capacity; ++i) p_list.Add(p_plus1 ? i + 1 : i); }
        public static int PopRandom(in List<int> p_list)
        { int i = random.Next(p_list.Count); int _num = p_list[i]; p_list.RemoveAt(i); return _num; }
        //public static void InitRandom(in int[] p_arr, in List<int> p_inputs, in bool p_plus1)
        //{
        //    Init(p_inputs, p_plus1); for (int i = 0; i < p_arr.Length; ++i)
        //    { if (p_inputs.Count == 0) Init(p_inputs, p_plus1); p_arr[i] = PopRandom(p_inputs); }
        //}
        //public static void InitRandom(in int[] p_arr, in List<int> p_inputs, in bool p_plus1)
        //{
        //    Init(p_inputs, p_plus1); for (int i = 0; i < p_arr.Length; ++i)
        //    {
        //        if (i > 0 && i % p_inputs.Count == 0) Init(p_inputs, p_plus1);
        //        p_arr[i] = p_inputs[i % p_inputs.Count];
        //    }
        //    for (int r, t, i = 0; i < p_arr.Length; ++i)
        //    {
        //        int a_rows = i / p_inputs.Count, a_start = a_rows * p_inputs.Count, a_nextStart = (a_rows + 1) * p_inputs.Count;
        //        r = random.Next(a_start, a_nextStart); (p_arr[i], p_arr[r]) = (p_arr[r], p_arr[i]);
        //    }
        //}
        public static void InitRandom(in int[] p_arr, in int p_length, in bool p_plus1)
        {
            for (int i = 0; i < p_arr.Length; ++i) p_arr[i] = i % p_length + (p_plus1 ? 1 : 0);
            for (int r, t, i = 0; i < p_arr.Length; ++i)
            {
                int a_rows = i / p_length, a_start = a_rows * p_length, a_nextStart = (a_rows + 1) * p_length;
                r = random.Next(a_start, a_nextStart); (p_arr[i], p_arr[r]) = (p_arr[r], p_arr[i]);
            }
        }
    }
}
