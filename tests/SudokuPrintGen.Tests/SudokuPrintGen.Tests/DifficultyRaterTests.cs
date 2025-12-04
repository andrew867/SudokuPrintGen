using SudokuPrintGen.Core.Puzzle;
using SudokuPrintGen.Core.Solver;
using Xunit;

namespace SudokuPrintGen.Tests;

public class DifficultyRaterTests
{
    [Fact]
    public void RatePuzzle_ValidPuzzle_ReturnsRating()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        var solver = new DpllSolver();
        
        var rating = DifficultyRater.RatePuzzle(puzzle, solver);
        
        Assert.NotNull(rating);
        Assert.True(rating.ClueCount > 0);
        Assert.True(rating.EmptyCells > 0);
    }
    
    [Fact]
    public void RatePuzzle_SetsEstimatedDifficulty()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        var solver = new DpllSolver();
        
        var rating = DifficultyRater.RatePuzzle(puzzle, solver);
        
        Assert.True(Enum.IsDefined(typeof(Difficulty), rating.EstimatedDifficulty));
    }
}

