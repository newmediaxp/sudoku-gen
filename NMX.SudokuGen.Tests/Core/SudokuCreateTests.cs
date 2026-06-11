namespace NMX.SudokuGen.Tests.Core;
using NMX.SudokuGen.Library.Core;
using static NMX.SudokuGen.Library.Core.Utility;

public sealed class SudokuCreateTests
{
    [Theory]
    [InlineData(2, 4)]
    [InlineData(3, 27)]
    public void Create_SolutionIsValidGrid(int p_rank, int p_remove)
    {
        Sudoku a_sudoku = Sudoku.Create(p_rank, p_remove);
        GridAssert.ValidGrid(a_sudoku.Solution, p_rank);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(3, 27)]
    public void Create_PuzzleMatchesSolutionWhereFilled(int p_rank, int p_remove)
    {
        Sudoku a_sudoku = Sudoku.Create(p_rank, p_remove);
        GridAssert.PuzzleMatchesSolution(a_sudoku.Puzzle, a_sudoku.Solution);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(3, 27)]
    [InlineData(3, 81)]
    public void Create_BlankCountEqualsRemoved_AndNeverExceedsRequested(int p_rank, int p_remove)
    {
        Sudoku a_sudoku = Sudoku.Create(p_rank, p_remove);
        Assert.Equal(a_sudoku.Removed, Count(a_sudoku.Puzzle, 0));
        Assert.InRange(a_sudoku.Removed, 0, p_remove);
    }

    [Fact]
    public void Create_RemoveZero_PuzzleEqualsSolution()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 0);
        Assert.Equal(0, a_sudoku.Removed);
        Assert.True(Same(a_sudoku.Puzzle, a_sudoku.Solution));
    }

    [Fact]
    public void Create_PrunedPuzzleStaysUniquelySolvable()
    {
        Sudoku a_sudoku = Sudoku.Create(3, 54);
        Sudoku a_solved = Sudoku.Solve(a_sudoku.Puzzle);
        Assert.True(Same(a_sudoku.Solution, a_solved.Solution));
    }

    [Fact]
    public void Create_ConsecutiveCalls_ProduceDifferentSolutions()
    {
        Sudoku a_sudoku1 = Sudoku.Create(3, 0);
        Sudoku a_sudoku2 = Sudoku.Create(3, 0);
        Assert.False(Same(a_sudoku1.Solution, a_sudoku2.Solution));
    }

    [Theory]
    [InlineData(2, 6, 12345)]
    [InlineData(3, 40, 12345)]
    public void Create_SameSeed_ReproducesSamePuzzle(int p_rank, int p_remove, int p_seed)
    {
        Sudoku a_sudoku1 = Sudoku.Create(p_rank, p_remove, p_seed);
        Sudoku a_sudoku2 = Sudoku.Create(p_rank, p_remove, p_seed);
        Assert.True(Same(a_sudoku1.Solution, a_sudoku2.Solution));
        Assert.True(Same(a_sudoku1.Puzzle, a_sudoku2.Puzzle));
    }

    [Fact]
    public void Create_DifferentSeeds_ProduceDifferentSolutions()
    {
        Sudoku a_sudoku1 = Sudoku.Create(3, 0, 1);
        Sudoku a_sudoku2 = Sudoku.Create(3, 0, 2);
        Assert.False(Same(a_sudoku1.Solution, a_sudoku2.Solution));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void Create_RankOutOfBounds_Throws(int p_rank)
        => Assert.Throws<ArgumentException>(() => Sudoku.Create(p_rank, 4));

    [Fact]
    public void Create_NegativeBlanks_Throws()
        => Assert.Throws<ArgumentException>(() => Sudoku.Create(3, -1));

    [Fact]
    public void Create_Rank4_SolutionIsValidAndUniquelySolvable()
    {
        Sudoku a_sudoku = Sudoku.Create(4, 32);
        GridAssert.ValidGrid(a_sudoku.Solution, 4);
        GridAssert.PuzzleMatchesSolution(a_sudoku.Puzzle, a_sudoku.Solution);
        Assert.True(Same(a_sudoku.Solution, Sudoku.Solve(a_sudoku.Puzzle).Solution));
    }

    [Fact]
    public void Create_InParallel_ProducesValidUniquePuzzles()
    {
        Sudoku[] a_sudokus = new Sudoku[16];
        System.Threading.Tasks.Parallel.For(0, a_sudokus.Length,
            p_idx => a_sudokus[p_idx] = Sudoku.Create(3, 40));
        foreach (Sudoku a_sudoku in a_sudokus)
        {
            GridAssert.ValidGrid(a_sudoku.Solution, 3);
            GridAssert.PuzzleMatchesSolution(a_sudoku.Puzzle, a_sudoku.Solution);
            Assert.True(Same(a_sudoku.Solution, Sudoku.Solve(a_sudoku.Puzzle).Solution));
        }
    }
}
