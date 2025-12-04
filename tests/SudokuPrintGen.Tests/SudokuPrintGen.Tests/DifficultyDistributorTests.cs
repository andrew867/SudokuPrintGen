using SudokuPrintGen.Core.Puzzle;
using Xunit;

namespace SudokuPrintGen.Tests;

public class DifficultyDistributorTests
{
    [Fact]
    public void ParseDifficulties_SingleDifficulty_ReturnsList()
    {
        var result = DifficultyDistributor.ParseDifficulties("Easy");
        Assert.Single(result);
        Assert.Equal(Difficulty.Easy, result[0]);
    }
    
    [Fact]
    public void ParseDifficulties_MultipleDifficulties_ReturnsList()
    {
        var result = DifficultyDistributor.ParseDifficulties("Easy,Medium,Hard");
        Assert.Equal(3, result.Count);
        Assert.Equal(Difficulty.Easy, result[0]);
        Assert.Equal(Difficulty.Medium, result[1]);
        Assert.Equal(Difficulty.Hard, result[2]);
    }
    
    [Fact]
    public void DistributeDifficulties_TwoDifficulties_SixPuzzles_Alternates()
    {
        var difficulties = new List<Difficulty> { Difficulty.Easy, Difficulty.Medium };
        var result = DifficultyDistributor.DistributeDifficulties(difficulties, 6);
        
        Assert.Equal(6, result.Count);
        // Should be: Easy, Easy, Medium, Medium, Easy, Easy (groups of 2)
        Assert.Equal(Difficulty.Easy, result[0]);
        Assert.Equal(Difficulty.Easy, result[1]);
        Assert.Equal(Difficulty.Medium, result[2]);
        Assert.Equal(Difficulty.Medium, result[3]);
        Assert.Equal(Difficulty.Easy, result[4]);
        Assert.Equal(Difficulty.Easy, result[5]);
    }
    
    [Fact]
    public void DistributeDifficulties_TwoDifficulties_FivePuzzles_UsesFirstMore()
    {
        var difficulties = new List<Difficulty> { Difficulty.Easy, Difficulty.Medium };
        var result = DifficultyDistributor.DistributeDifficulties(difficulties, 5);
        
        Assert.Equal(5, result.Count);
        // Should be: Easy, Easy, Medium, Medium, Easy (3 easy, 2 medium)
        Assert.Equal(3, result.Count(d => d == Difficulty.Easy));
        Assert.Equal(2, result.Count(d => d == Difficulty.Medium));
    }
    
    [Fact]
    public void DistributeDifficulties_ThreeDifficulties_CyclesThrough()
    {
        var difficulties = new List<Difficulty> { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };
        var result = DifficultyDistributor.DistributeDifficulties(difficulties, 9);
        
        Assert.Equal(9, result.Count);
        // Should cycle: Easy, Easy, Medium, Medium, Hard, Hard, Easy, Easy, Medium
        Assert.Equal(3, result.Count(d => d == Difficulty.Easy));
        Assert.Equal(3, result.Count(d => d == Difficulty.Medium));
        Assert.Equal(3, result.Count(d => d == Difficulty.Hard));
    }
}

