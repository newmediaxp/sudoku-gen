namespace NMX.SudokuGen.Tests.Core;
using NMX.SudokuGen.Library.Core;
using static NMX.SudokuGen.Library.Core.Utility;

public sealed class SudokuSolveTests
{
    // known pair taken from the console app's help examples
    private const string knownPuzz =
        "390560027.001809405.600172893.180257900.500643000.204081570.003096052.050328619.962010300";
    private const string knownSoln =
        "398564127.721839465.645172893.186257934.579643281.234981576.813496752.457328619.962715348";

    [Theory]
    [InlineData(2, 6)]
    [InlineData(3, 40)]
    public void Solve_RoundTripsCreatedPuzzle(int p_rank, int p_remove)
    {
        Sudoku a_created = Sudoku.Create(p_rank, p_remove);
        Sudoku a_solved = Sudoku.Solve(a_created.Puzzle);
        Assert.True(Same(a_created.Solution, a_solved.Solution));
    }

    [Fact]
    public void Solve_KnownPuzzle_GivesKnownSolution()
    {
        Sudoku a_sudoku = Sudoku.Solve(GetPuzzleArr(knownPuzz));
        Assert.Equal(GetPuzzleArr(knownSoln), a_sudoku.Solution);
        GridAssert.ValidGrid(a_sudoku.Solution, 3);
    }

    [Fact]
    public void Solve_CompleteGrid_ReturnsItUnchanged()
    {
        int[] a_grid = GetPuzzleArr(knownSoln);
        Sudoku a_sudoku = Sudoku.Solve(a_grid);
        Assert.Equal(a_grid, a_sudoku.Solution);
        Assert.Equal(0, a_sudoku.Removed);
    }

    [Fact]
    public void Solve_InfersRankFromLength()
    {
        Sudoku a_created = Sudoku.Create(2, 6);
        Assert.Equal(2, Sudoku.Solve(a_created.Puzzle).rank);
    }

    [Fact]
    public void Solve_InvalidLength_Throws()
        => Assert.Throws<InvalidOperationException>(() => Sudoku.Solve(new int[10]));

    [Fact]
    public void Solve_DuplicateInRow_Throws()
    {
        int[] a_puzz = new int[81];
        a_puzz[0] = 5; a_puzz[1] = 5;
        Assert.Throws<InvalidOperationException>(() => Sudoku.Solve(a_puzz));
    }

    [Fact]
    public void Solve_DuplicateInColumn_Throws()
    {
        int[] a_puzz = new int[81];
        a_puzz[0] = 5; a_puzz[9] = 5;
        Assert.Throws<InvalidOperationException>(() => Sudoku.Solve(a_puzz));
    }

    [Fact]
    public void Solve_DuplicateInSegment_Throws()
    {
        int[] a_puzz = new int[81];
        a_puzz[0] = 5; a_puzz[10] = 5;
        Assert.Throws<InvalidOperationException>(() => Sudoku.Solve(a_puzz));
    }

    [Fact]
    public void Solve_ValueOutOfRange_Throws()
    {
        int[] a_puzz = new int[81];
        a_puzz[0] = 10;
        Assert.Throws<InvalidOperationException>(() => Sudoku.Solve(a_puzz));
    }

    [Fact]
    public void Solve_NonUniquePuzzle_Throws()
    {
        // an empty grid has many solutions, so it must be rejected
        InvalidOperationException a_ex =
            Assert.Throws<InvalidOperationException>(() => Sudoku.Solve(new int[81]));
        Assert.Contains("unique", a_ex.Message);
    }
}
