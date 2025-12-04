using SudokuPrintGen.Core.Puzzle;
using Xunit;

namespace SudokuPrintGen.Tests;

public class BoardTests
{
    [Fact]
    public void CreateStandard_Returns9x9Board()
    {
        var board = Board.CreateStandard();
        Assert.Equal(9, board.Size);
        Assert.Equal(3, board.BoxRows);
        Assert.Equal(3, board.BoxCols);
    }
    
    [Fact]
    public void FromString_ParsesCorrectly()
    {
        var puzzle = "530070000600195000098000060800060003400803001700020006060000280000419005000080079";
        var board = Board.FromString(puzzle);
        
        Assert.Equal(9, board.Size);
        Assert.Equal(5, board[0, 0]);
        Assert.Equal(3, board[0, 1]);
        Assert.Equal(0, board[0, 2]);
    }
    
    [Fact]
    public void ToString_ProducesCorrectFormat()
    {
        var board = Board.CreateStandard();
        board[0, 0] = 5;
        board[0, 1] = 3;
        
        var str = board.ToString();
        Assert.StartsWith("53", str);
    }
    
    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var board = Board.CreateStandard();
        board[0, 0] = 5;
        
        var clone = board.Clone();
        clone[0, 0] = 9;
        
        Assert.Equal(5, board[0, 0]);
        Assert.Equal(9, clone[0, 0]);
    }
    
    [Fact]
    public void GetClueCount_CountsFilledCells()
    {
        var board = Board.CreateStandard();
        board[0, 0] = 5;
        board[0, 1] = 3;
        
        Assert.Equal(2, board.GetClueCount());
    }
}

