// Copyright (c) 2024-2026 New Media XP. Licensed under the MIT License.

namespace NMX.SudokuGen.Tests.Core;
using NMX.SudokuGen.Library.Core;
using static NMX.SudokuGen.Library.Core.Utility;

public sealed class SudokuShuffleTests
{
    [Theory]
    [InlineData(2, 6)]
    [InlineData(3, 40)]
    public void Shuffle_ProducesValidGrid(int p_rank, int p_remove)
    {
        Sudoku a_shuffled = Sudoku.Create(p_rank, p_remove).Shuffle();
        GridAssert.ValidGrid(a_shuffled.Solution, p_rank);
        GridAssert.PuzzleMatchesSolution(a_shuffled.Puzzle, a_shuffled.Solution);
    }

    [Fact]
    public void Shuffle_PreservesRankAndBlankCount()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 40);
        Sudoku a_shuffled = a_sudoku.Shuffle();
        Assert.Equal(a_sudoku.rank, a_shuffled.rank);
        Assert.Equal(a_sudoku.Removed, a_shuffled.Removed);
        Assert.Equal(Count(a_sudoku.Puzzle, 0), Count(a_shuffled.Puzzle, 0));
    }

    [Theory]
    [InlineData(2, 6)]
    [InlineData(3, 40)]
    public void Shuffle_StaysUniquelySolvable(int p_rank, int p_remove)
    {
        Sudoku a_shuffled = Sudoku.Create(p_rank, p_remove).Shuffle();
        Assert.True(Same(a_shuffled.Solution, Sudoku.Solve(a_shuffled.Puzzle).Solution));
    }

    [Fact]
    public void Shuffle_SameSeed_ReproducesSameResult()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 40, 12345);
        Sudoku a_shuffled1 = a_sudoku.Shuffle(777), a_shuffled2 = a_sudoku.Shuffle(777);
        Assert.True(Same(a_shuffled1.Puzzle, a_shuffled2.Puzzle));
        Assert.True(Same(a_shuffled1.Solution, a_shuffled2.Solution));
    }

    [Fact]
    public void Shuffle_DifferentSeeds_ProduceDifferentResults()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 40, 12345);
        Assert.False(Same(a_sudoku.Shuffle(1).Solution, a_sudoku.Shuffle(2).Solution));
    }

    [Fact]
    public void Shuffle_ProducesDifferentBoard()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 40);
        Assert.False(Same(a_sudoku.Solution, a_sudoku.Shuffle().Solution));
    }

    [Fact]
    public void Shuffle_DoesNotChangeOriginal()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 40, 12345);
        int[] a_puzzBefore = [.. a_sudoku.Puzzle], a_solnBefore = [.. a_sudoku.Solution];
        a_sudoku.Shuffle();
        Assert.True(Same(a_puzzBefore, a_sudoku.Puzzle));
        Assert.True(Same(a_solnBefore, a_sudoku.Solution));
    }
}
