// Copyright (c) 2024-2026 New Media XP. Licensed under the MIT License.

namespace NMX.SudokuGen.Tests.Core;

internal static class GridAssert
{
    // asserts every row, column and segment of p_grid contains 1..rows exactly once
    public static void ValidGrid(in IReadOnlyList<int> p_grid, in int p_rank)
    {
        int a_rows = p_rank * p_rank;
        Assert.Equal(a_rows * a_rows, p_grid.Count);
        for (int i_row = 0; i_row < a_rows; ++i_row)
        {
            bool[] a_seen = new bool[a_rows + 1];
            for (int i = 0; i < a_rows; ++i)
            {
                int a_input = p_grid[i_row * a_rows + i];
                Assert.InRange(a_input, 1, a_rows);
                Assert.False(a_seen[a_input], $"duplicate {a_input} in row {i_row}");
                a_seen[a_input] = true;
            }
        }
        for (int i_col = 0; i_col < a_rows; ++i_col)
        {
            bool[] a_seen = new bool[a_rows + 1];
            for (int i = 0; i < a_rows; ++i)
            {
                int a_input = p_grid[i * a_rows + i_col];
                Assert.False(a_seen[a_input], $"duplicate {a_input} in column {i_col}");
                a_seen[a_input] = true;
            }
        }
        for (int i_seg = 0; i_seg < a_rows; ++i_seg)
        {
            bool[] a_seen = new bool[a_rows + 1];
            int a_start = i_seg / p_rank * p_rank * a_rows + i_seg % p_rank * p_rank;
            for (int i = 0; i < a_rows; ++i)
            {
                int a_input = p_grid[a_start + i / p_rank * a_rows + i % p_rank];
                Assert.False(a_seen[a_input], $"duplicate {a_input} in segment {i_seg}");
                a_seen[a_input] = true;
            }
        }
    }

    // asserts every non-blank square of p_puzz equals the corresponding square of p_soln
    public static void PuzzleMatchesSolution(in IReadOnlyList<int> p_puzz, in IReadOnlyList<int> p_soln)
    {
        Assert.Equal(p_soln.Count, p_puzz.Count);
        for (int i = 0; i < p_puzz.Count; ++i)
            if (p_puzz[i] != 0) Assert.Equal(p_soln[i], p_puzz[i]);
    }
}
