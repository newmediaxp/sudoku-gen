namespace NMX.SudokuGen.Tests.Core;
using NMX.SudokuGen.Library.Core;

public sealed class SudokuConflictTests
{
    private static readonly Sudoku sudoku = Sudoku.Create(3, 40, 12345);

    [Fact]
    public void FindConflicts_BlankSquare_ReturnsNone()
        => Assert.Equal(Sudoku.Conflict.None, sudoku.FindConflicts(new int[81], 0));

    [Fact]
    public void FindConflicts_LoneValue_ReturnsNone()
    {
        int[] a_board = new int[81];
        a_board[0] = 5;
        Assert.Equal(Sudoku.Conflict.None, sudoku.FindConflicts(a_board, 0));
    }

    [Fact]
    public void FindConflicts_SameRowOtherSegment_ReturnsRow()
    {
        int[] a_board = new int[81];
        a_board[0] = 5; a_board[8] = 5;
        Assert.Equal(Sudoku.Conflict.Row, sudoku.FindConflicts(a_board, 0));
    }

    [Fact]
    public void FindConflicts_SameColumnOtherSegment_ReturnsColumn()
    {
        int[] a_board = new int[81];
        a_board[0] = 5; a_board[72] = 5;
        Assert.Equal(Sudoku.Conflict.Column, sudoku.FindConflicts(a_board, 0));
    }

    [Fact]
    public void FindConflicts_SameSegmentOtherRowCol_ReturnsSegment()
    {
        int[] a_board = new int[81];
        a_board[0] = 5; a_board[10] = 5;
        Assert.Equal(Sudoku.Conflict.Segment, sudoku.FindConflicts(a_board, 0));
    }

    [Fact]
    public void FindConflicts_NeighbourInRowAndSegment_ReturnsBoth()
    {
        int[] a_board = new int[81];
        a_board[0] = 5; a_board[1] = 5;
        Assert.Equal(Sudoku.Conflict.Row | Sudoku.Conflict.Segment, sudoku.FindConflicts(a_board, 0));
    }

    [Fact]
    public void FindConflicts_AllThreeUnits_ReturnsAll()
    {
        int[] a_board = new int[81];
        a_board[0] = 5; a_board[8] = 5; a_board[72] = 5; a_board[10] = 5;
        Assert.Equal(Sudoku.Conflict.Row | Sudoku.Conflict.Column | Sudoku.Conflict.Segment,
            sudoku.FindConflicts(a_board, 0));
    }

    [Fact]
    public void FindConflicts_WrongBoardLength_Throws()
        => Assert.Throws<ArgumentException>(() => sudoku.FindConflicts(new int[16], 0));

    [Theory]
    [InlineData(-1)]
    [InlineData(81)]
    public void FindConflicts_IndexOutOfBoard_Throws(int p_idx)
        => Assert.Throws<ArgumentException>(() => sudoku.FindConflicts(new int[81], p_idx));

    [Fact]
    public void FindConflicts_ValueOutOfRange_Throws()
    {
        int[] a_board = new int[81];
        a_board[0] = 10;
        Assert.Throws<ArgumentException>(() => sudoku.FindConflicts(a_board, 0));
    }
}
