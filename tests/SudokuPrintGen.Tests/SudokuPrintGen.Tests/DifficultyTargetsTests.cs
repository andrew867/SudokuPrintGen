using SudokuPrintGen.Core.Puzzle;
using Xunit;

namespace SudokuPrintGen.Tests;

/// <summary>
/// Tests for difficulty target configuration.
/// </summary>
public class DifficultyTargetsTests
{
    [Theory]
    [InlineData(Difficulty.Easy, 5)]
    [InlineData(Difficulty.Medium, 15)]
    [InlineData(Difficulty.Hard, 40)]
    [InlineData(Difficulty.Expert, 150)]
    [InlineData(Difficulty.Evil, 400)]
    public void GetIterationGoal_ReturnsExpectedGoal(Difficulty difficulty, int expectedGoal)
    {
        var goal = DifficultyTargets.GetIterationGoal(difficulty);
        
        Assert.Equal(expectedGoal, goal);
    }
    
    [Theory]
    [InlineData(Difficulty.Easy, 1, 10)]
    [InlineData(Difficulty.Medium, 11, 25)]
    [InlineData(Difficulty.Hard, 26, 80)]
    [InlineData(Difficulty.Expert, 81, 350)]
    public void GetIterationRange_ReturnsExpectedRange(Difficulty difficulty, int expectedMin, int expectedMax)
    {
        var (min, max) = DifficultyTargets.GetIterationRange(difficulty);
        
        Assert.Equal(expectedMin, min);
        Assert.Equal(expectedMax, max);
    }
    
    [Theory]
    [InlineData(5, Difficulty.Easy, true)]
    [InlineData(15, Difficulty.Medium, true)]
    [InlineData(50, Difficulty.Hard, true)]
    [InlineData(100, Difficulty.Expert, true)]
    [InlineData(500, Difficulty.Evil, true)]
    [InlineData(5, Difficulty.Hard, false)]  // Too easy for Hard
    [InlineData(100, Difficulty.Easy, false)] // Too hard for Easy
    public void IsInRange_ReturnsCorrectResult(int iterations, Difficulty difficulty, bool expected)
    {
        var result = DifficultyTargets.IsInRange(iterations, difficulty);
        
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData(3, Difficulty.Easy)]
    [InlineData(15, Difficulty.Medium)]
    [InlineData(50, Difficulty.Hard)]
    [InlineData(200, Difficulty.Expert)]
    [InlineData(500, Difficulty.Evil)]
    public void GetDifficultyFromIterations_ReturnsCorrectDifficulty(int iterations, Difficulty expected)
    {
        var result = DifficultyTargets.GetDifficultyFromIterations(iterations);
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void GetRelativeDeviation_ZeroAtGoal()
    {
        var goal = DifficultyTargets.GetIterationGoal(Difficulty.Medium);
        var deviation = DifficultyTargets.GetRelativeDeviation(goal, Difficulty.Medium);
        
        Assert.Equal(0.0, deviation, 0.001);
    }
    
    [Fact]
    public void GetRelativeDeviation_PositiveWhenDifferent()
    {
        var goal = DifficultyTargets.GetIterationGoal(Difficulty.Medium);
        var deviation = DifficultyTargets.GetRelativeDeviation(goal * 2, Difficulty.Medium);
        
        Assert.True(deviation > 0);
    }
    
    [Theory]
    [InlineData(5, Difficulty.Easy, DifficultyComparison.InRange)]
    [InlineData(1, Difficulty.Medium, DifficultyComparison.TooEasy)]
    [InlineData(500, Difficulty.Easy, DifficultyComparison.TooHard)]
    public void CompareToDifficulty_ReturnsCorrectComparison(int iterations, Difficulty target, DifficultyComparison expected)
    {
        var result = DifficultyTargets.CompareToDifficulty(iterations, target);
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void IsCloseToTarget_TrueWithinTolerance()
    {
        var goal = DifficultyTargets.GetIterationGoal(Difficulty.Medium);
        var closeValue = (int)(goal * 1.3); // 30% above goal
        
        var result = DifficultyTargets.IsCloseToTarget(closeValue, Difficulty.Medium, 0.5, 5);
        
        Assert.True(result);
    }
    
    [Fact]
    public void IsCloseToTarget_FalseOutsideTolerance()
    {
        var goal = DifficultyTargets.GetIterationGoal(Difficulty.Easy);
        var farValue = goal * 10; // 10x the goal
        
        var result = DifficultyTargets.IsCloseToTarget(farValue, Difficulty.Easy, 0.15, 3);
        
        Assert.False(result);
    }
    
    [Fact]
    public void GetScoreRange_AllDifficultiesHaveRanges()
    {
        foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
        {
            var (min, max) = DifficultyTargets.GetScoreRange(difficulty);
            
            Assert.True(min >= 0);
            Assert.True(max > min);
        }
    }
    
    [Fact]
    public void ScoreRanges_AreContiguous()
    {
        var difficulties = Enum.GetValues<Difficulty>();
        
        for (int i = 0; i < difficulties.Length - 1; i++)
        {
            var (_, max1) = DifficultyTargets.GetScoreRange((Difficulty)difficulties.GetValue(i)!);
            var (min2, _) = DifficultyTargets.GetScoreRange((Difficulty)difficulties.GetValue(i + 1)!);
            
            // Ranges should be contiguous (max of one equals min of next)
            Assert.Equal(max1, min2);
        }
    }
}

