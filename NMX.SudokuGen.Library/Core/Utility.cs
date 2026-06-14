// Copyright (c) 2024-2026 New Media XP. Licensed under the MIT License.

namespace NMX.SudokuGen.Library.Core;

using System;
using System.Collections.Generic;
using System.Text;

public static class Utility
{
    /// <summary>
    /// Copies one array into another, up to the destination's length (not the source's).
    /// </summary>
    /// <param name="p_from">Source array; must be at least as long as the destination.</param>
    /// <param name="p_to">Destination array; filled completely.</param>
    public static void Copy(in int[] p_from, in int[] p_to) => Array.Copy(p_from, 0, p_to, 0, p_to.Length);

    public static bool Same(in IReadOnlyList<int> p_arr1, in IReadOnlyList<int> p_arr2)
    { for (int i = 0; i < p_arr1.Count; ++i) if (p_arr1[i] != p_arr2[i]) return false; return true; }

    public static int Count(in IReadOnlyList<int> p_arr, in int p_num)
    { int a_count = 0; for (int i = 0; i < p_arr.Count; ++i) if (p_arr[i] == p_num) ++a_count; return a_count; }

    public static void Swap2(ref int p_num1, ref int p_num2) => (p_num2, p_num1) = (p_num1, p_num2);

    public static void Init(in int[] p_arr) { for (int i = 0; i < p_arr.Length; ++i) p_arr[i] = 0; }

    /// <summary>
    /// Fills an array block-wise so that every consecutive block ends up a random permutation of the same value range.
    /// </summary>
    /// <param name="p_random">Random generator driving the shuffle.</param>
    /// <param name="p_arr">Array to fill; its length should be a multiple of <paramref name="p_length"/>.</param>
    /// <param name="p_length">Block size, and the size of the permuted value range.</param>
    /// <param name="p_plus1">When true each block holds 1..length, otherwise 0..length-1.</param>
    public static void InitRandom(in Random p_random, in int[] p_arr, in int p_length, in bool p_plus1)
    {
        for (int i = 0; i < p_arr.Length; ++i) p_arr[i] = i % p_length + (p_plus1 ? 1 : 0);
        for (int r, i = 0; i < p_arr.Length; ++i)
        {
            int a_rows = i / p_length, a_start = a_rows * p_length, a_nextStart = (a_rows + 1) * p_length;
            r = p_random.Next(a_start, a_nextStart); Swap2(ref p_arr[i], ref p_arr[r]);
        }
    }

    /// <summary>
    /// Counts the set bits of <paramref name="p_bits"/> (SWAR popcount;
    /// netstandard2.1 offers no <c>BitOperations.PopCount</c>).
    /// </summary>
    /// <param name="p_bits">The bits to count.</param>
    /// <returns>The number of set bits, 0..32.</returns>
    public static int PopCount(in uint p_bits)
    {
        uint a_bits = p_bits;
        a_bits -= (a_bits >> 1) & 0x55555555u;
        a_bits = (a_bits & 0x33333333u) + ((a_bits >> 2) & 0x33333333u);
        return (int)((((a_bits + (a_bits >> 4)) & 0x0F0F0F0Fu) * 0x01010101u) >> 24);
    }

    /// <summary>
    /// Encodes a grid as one character per square, row by row: 0 = blank, 1-9 as digits,
    /// 10 and above as letters ('A' = 10), '.' between rows.
    /// Values above 35 ('Z') are unrepresentable, so rank 5 (25x25) at most.
    /// </summary>
    /// <param name="p_puzz">The grid to encode, row by row.</param>
    /// <returns>The puzzle code, e.g. "0012.3400...".</returns>
    public static string GetPuzzleCode(in IReadOnlyList<int> p_puzz)
    {
        int a_rows = (int)Math.Sqrt(p_puzz.Count);
        StringBuilder a_puzzleCode = new(p_puzz.Count + a_rows);
        for (int i = 0; i < p_puzz.Count; ++i)
        {
            if (i > 0 && i % a_rows == 0) a_puzzleCode.Append('.');
            a_puzzleCode.Append(p_puzz[i] > 9 ? (char)('A' + p_puzz[i] - 10) : (char)('0' + p_puzz[i]));
        }
        return a_puzzleCode.ToString();
    }

    /// <summary>
    /// Parses a puzzle code back into a grid array.
    /// </summary>
    /// <param name="p_code">Text containing the grid as digits and letters ('A' or 'a' = 10);
    /// every other character is ignored.</param>
    /// <returns>One element per recognised character, in order.</returns>
    public static int[] GetPuzzleArr(in string p_code)
    {
        List<int> a_puzzle = new(p_code.Length);
        foreach (char c in p_code)
        {
            int a_value = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'A' and <= 'Z' => c - 'A' + 10,
                >= 'a' and <= 'z' => c - 'a' + 10,
                _ => -1,
            };
            if (a_value != -1) a_puzzle.Add(a_value);
        }
        return [.. a_puzzle];
    }
}
