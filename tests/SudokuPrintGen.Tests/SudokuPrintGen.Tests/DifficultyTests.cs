using SudokuPrintGen.Core.Puzzle;
using Xunit;

namespace SudokuPrintGen.Tests;

public class DifficultyTests
{
    [Theory]
    [InlineData(Difficulty.Easy, 9, 40)]
    [InlineData(Difficulty.Medium, 9, 32)]
    [InlineData(Difficulty.Hard, 9, 26)]
    [InlineData(Difficulty.Expert, 9, 20)]
    [InlineData(Difficulty.Evil, 9, 17)]
    public void GetTargetClues_ReturnsExpectedCount(Difficulty difficulty, int size, int expectedMin)
    {
        var clues = difficulty.GetTargetClues(size);
        
        // Allow some variance, but should be close to expected
        Assert.True(clues >= expectedMin - 2 && clues <= expectedMin + 5);
    }
    
    [Fact]
    public void GetTargetClues_9x9Evil_AtLeast17()
    {
        var clues = Difficulty.Evil.GetTargetClues(9);
        Assert.True(clues >= 17); // Minimum for unique solution
    }
}

