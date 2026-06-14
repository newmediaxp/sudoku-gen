// Copyright (c) 2024-2026 New Media XP. Licensed under the MIT License.

namespace NMX.SudokuGen.Tests.Core;
using static NMX.SudokuGen.Library.Core.Utility;

public sealed class UtilityTests
{
    [Fact]
    public void Init_ZeroesAllElements()
    {
        int[] a_arr = [1, 2, 3, 4, 5];
        Init(a_arr);
        Assert.All(a_arr, p_num => Assert.Equal(0, p_num));
    }

    [Fact]
    public void Copy_CopiesDestinationLengthElements()
    {
        int[] a_from = [1, 2, 3, 4, 5];
        int[] a_to = new int[5];
        Copy(a_from, a_to);
        Assert.Equal(a_from, a_to);
    }

    [Fact]
    public void Same_TrueForEqualArrays()
    {
        Assert.True(Same([1, 2, 3], [1, 2, 3]));
        Assert.False(Same([1, 2, 3], [1, 2, 4]));
    }

    [Fact]
    public void Count_CountsMatchingElements()
    {
        Assert.Equal(3, Count([0, 1, 0, 2, 0], 0));
        Assert.Equal(0, Count([1, 2, 3], 9));
    }

    [Fact]
    public void Swap2_SwapsValues()
    {
        int a_num1 = 1, a_num2 = 2;
        Swap2(ref a_num1, ref a_num2);
        Assert.Equal(2, a_num1);
        Assert.Equal(1, a_num2);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void InitRandom_EachBlockIsPermutation(bool p_plus1, int p_offset)
    {
        const int a_length = 9, a_blocks = 3;
        int[] a_arr = new int[a_length * a_blocks];
        InitRandom(new Random(), a_arr, a_length, p_plus1);
        for (int i_block = 0; i_block < a_blocks; ++i_block)
        {
            bool[] a_seen = new bool[a_length];
            for (int i = 0; i < a_length; ++i)
            {
                int a_num = a_arr[i_block * a_length + i] - p_offset;
                Assert.InRange(a_num, 0, a_length - 1);
                Assert.False(a_seen[a_num], $"duplicate {a_num} in block {i_block}");
                a_seen[a_num] = true;
            }
        }
    }

    [Fact]
    public void PuzzleCode_RoundTrips()
    {
        int[] a_puzz = new int[81];
        for (int i = 0; i < a_puzz.Length; ++i) a_puzz[i] = i % 10;
        Assert.Equal(a_puzz, GetPuzzleArr(GetPuzzleCode(a_puzz)));
    }

    [Fact]
    public void GetPuzzleCode_SeparatesRowsWithDots()
    {
        int[] a_puzz = [1, 2, 3, 4];
        Assert.Equal("12.34", GetPuzzleCode(a_puzz));
    }

    [Fact]
    public void GetPuzzleArr_IgnoresUnrecognisedCharacters()
    {
        Assert.Equal(new int[] { 1, 0, 2 }, GetPuzzleArr("1+0.2-"));
    }

    [Fact]
    public void PuzzleCode_RoundTripsValuesAboveNine()
    {
        int[] a_puzz = new int[256];
        for (int i = 0; i < a_puzz.Length; ++i) a_puzz[i] = i % 17;
        string a_code = GetPuzzleCode(a_puzz);
        Assert.Contains('A', a_code);
        Assert.Equal(a_puzz, GetPuzzleArr(a_code));
    }

    [Fact]
    public void GetPuzzleArr_ParsesLettersCaseInsensitively()
    {
        Assert.Equal(new int[] { 10, 35, 10, 35 }, GetPuzzleArr("AZaz"));
    }
}
