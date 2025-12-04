using SudokuPrintGen.Core.Puzzle;
using Xunit;

namespace SudokuPrintGen.Tests;

public class ValidatorTests
{
    [Fact]
    public void Validate_ValidCompleteBoard_ReturnsValid()
    {
        var board = Board.FromString("534678912672195348198342567859761423426853791713924856961537284287419635345286179");
        var result = BoardValidator.Validate(board);
        
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
    
    [Fact]
    public void Validate_InvalidRow_ReturnsInvalid()
    {
        var board = Board.CreateStandard();
        board[0, 0] = 5;
        board[0, 1] = 5; // Duplicate in row
        
        var result = BoardValidator.Validate(board);
        
        Assert.False(result.IsValid);
        Assert.Contains("Duplicate", result.GetErrorMessage());
    }
    
    [Fact]
    public void Validate_InvalidColumn_ReturnsInvalid()
    {
        var board = Board.CreateStandard();
        board[0, 0] = 5;
        board[1, 0] = 5; // Duplicate in column
        
        var result = BoardValidator.Validate(board);
        
        Assert.False(result.IsValid);
        Assert.Contains("Duplicate", result.GetErrorMessage());
    }
    
    [Fact]
    public void Validate_InvalidBox_ReturnsInvalid()
    {
        var board = Board.CreateStandard();
        board[0, 0] = 5;
        board[1, 1] = 5; // Duplicate in box
        
        var result = BoardValidator.Validate(board);
        
        Assert.False(result.IsValid);
        Assert.Contains("Duplicate", result.GetErrorMessage());
    }
}

