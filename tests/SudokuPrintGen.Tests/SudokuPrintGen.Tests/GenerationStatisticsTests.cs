using SudokuPrintGen.Core.Puzzle;
using Xunit;

namespace SudokuPrintGen.Tests;

/// <summary>
/// Tests for generation statistics tracking.
/// </summary>
public class GenerationStatisticsTests
{
    [Fact]
    public void GenerationStatistics_Initial_IsEmpty()
    {
        var stats = new GenerationStatistics();
        
        Assert.Equal(0, stats.TotalPuzzles);
    }
    
    [Fact]
    public void GenerationStatistics_AddPuzzle_IncrementsCount()
    {
        var stats = new GenerationStatistics();
        var puzzle = CreateMockPuzzle(Difficulty.Easy);
        
        stats.AddPuzzle(puzzle);
        
        Assert.Equal(1, stats.TotalPuzzles);
    }
    
    [Fact]
    public void GenerationStatistics_PuzzlesByDifficulty_CountsCorrectly()
    {
        var stats = new GenerationStatistics();
        
        stats.AddPuzzle(CreateMockPuzzle(Difficulty.Easy));
        stats.AddPuzzle(CreateMockPuzzle(Difficulty.Easy));
        stats.AddPuzzle(CreateMockPuzzle(Difficulty.Medium));
        
        var byDifficulty = stats.PuzzlesByDifficulty;
        
        Assert.Equal(2, byDifficulty[Difficulty.Easy]);
        Assert.Equal(1, byDifficulty[Difficulty.Medium]);
    }
    
    [Fact]
    public void GenerationStatistics_Reset_ClearsAll()
    {
        var stats = new GenerationStatistics();
        stats.AddPuzzle(CreateMockPuzzle(Difficulty.Easy));
        stats.AddPuzzle(CreateMockPuzzle(Difficulty.Medium));
        
        stats.Reset();
        
        Assert.Equal(0, stats.TotalPuzzles);
    }
    
    [Fact]
    public void GenerationStatistics_GetReport_ReturnsString()
    {
        var stats = new GenerationStatistics();
        stats.AddPuzzle(CreateMockPuzzle(Difficulty.Easy));
        
        var report = stats.GetReport();
        
        Assert.NotNull(report);
        Assert.Contains("Statistics", report);
    }
    
    [Fact]
    public void GenerationStatistics_GetReport_EmptyStats_HandlesGracefully()
    {
        var stats = new GenerationStatistics();
        
        var report = stats.GetReport();
        
        Assert.NotNull(report);
        Assert.Contains("No puzzles generated", report);
    }
    
    [Fact]
    public void GenerationStatistics_AverageIterationCount_CalculatesCorrectly()
    {
        var stats = new GenerationStatistics();
        
        var puzzle1 = CreateMockPuzzle(Difficulty.Easy, iterationCount: 10);
        var puzzle2 = CreateMockPuzzle(Difficulty.Easy, iterationCount: 20);
        
        stats.AddPuzzle(puzzle1);
        stats.AddPuzzle(puzzle2);
        
        var avgIterations = stats.AverageIterationCount;
        
        Assert.Equal(15.0, avgIterations[Difficulty.Easy], 0.001);
    }
    
    [Fact]
    public void GenerationStatistics_SuccessRate_CalculatesPercentage()
    {
        var stats = new GenerationStatistics();
        
        var successPuzzle = CreateMockPuzzle(Difficulty.Easy, matchedTarget: true);
        var failPuzzle = CreateMockPuzzle(Difficulty.Easy, matchedTarget: false);
        
        stats.AddPuzzle(successPuzzle);
        stats.AddPuzzle(failPuzzle);
        
        var successRate = stats.SuccessRate;
        
        Assert.Equal(50.0, successRate[Difficulty.Easy], 0.001);
    }
    
    [Fact]
    public void GenerationStatistics_GetDetailedStats_ReturnsCompleteInfo()
    {
        var stats = new GenerationStatistics();
        stats.AddPuzzle(CreateMockPuzzle(Difficulty.Medium, iterationCount: 15, compositeScore: 12.5));
        stats.AddPuzzle(CreateMockPuzzle(Difficulty.Medium, iterationCount: 20, compositeScore: 18.0));
        
        var detailed = stats.GetDetailedStats(Difficulty.Medium);
        
        Assert.Equal(2, detailed.Count);
        Assert.Equal(17.5, detailed.AverageIterationCount, 0.001);
        Assert.Equal(15, detailed.MinIterationCount);
        Assert.Equal(20, detailed.MaxIterationCount);
    }
    
    [Fact]
    public void GenerationStatistics_GetDetailedStats_EmptyDifficulty_ReturnsEmptyStats()
    {
        var stats = new GenerationStatistics();
        stats.AddPuzzle(CreateMockPuzzle(Difficulty.Easy));
        
        var detailed = stats.GetDetailedStats(Difficulty.Hard);
        
        Assert.Equal(0, detailed.Count);
        Assert.Equal(Difficulty.Hard, detailed.Difficulty);
    }
    
    [Fact]
    public void PuzzleStats_StoresAllMetrics()
    {
        var puzzleStats = new PuzzleStats
        {
            TargetDifficulty = Difficulty.Medium,
            ActualDifficulty = Difficulty.Hard,
            IterationCount = 50,
            CompositeScore = 35.5,
            ClueCount = 28,
            MatchedTarget = false,
            RefinementIterations = 10,
            GuessCount = 5,
            MaxBacktrackDepth = 8
        };
        
        Assert.Equal(Difficulty.Medium, puzzleStats.TargetDifficulty);
        Assert.Equal(Difficulty.Hard, puzzleStats.ActualDifficulty);
        Assert.Equal(50, puzzleStats.IterationCount);
        Assert.Equal(35.5, puzzleStats.CompositeScore);
        Assert.Equal(28, puzzleStats.ClueCount);
        Assert.False(puzzleStats.MatchedTarget);
    }
    
    [Fact]
    public void DifficultyStatistics_ContainsAllMetrics()
    {
        var diffStats = new DifficultyStatistics
        {
            Difficulty = Difficulty.Expert,
            Count = 10,
            AverageIterationCount = 150.5,
            MinIterationCount = 85,
            MaxIterationCount = 300,
            IterationStdDev = 45.2,
            AverageCompositeScore = 120.0,
            SuccessCount = 8,
            SuccessRate = 80.0,
            AverageClueCount = 22.5,
            AverageGuessCount = 8.5,
            AverageBacktrackDepth = 12.0
        };
        
        Assert.Equal(Difficulty.Expert, diffStats.Difficulty);
        Assert.Equal(10, diffStats.Count);
        Assert.Equal(80.0, diffStats.SuccessRate);
    }
    
    private static GeneratedPuzzle CreateMockPuzzle(
        Difficulty difficulty, 
        int iterationCount = 10, 
        double compositeScore = 10.0,
        bool matchedTarget = true)
    {
        var board = new Board(9, 3, 3);
        
        return new GeneratedPuzzle
        {
            Puzzle = board,
            Solution = board,
            Difficulty = difficulty,
            DifficultyRating = new DifficultyRating
            {
                IterationCount = iterationCount,
                CompositeScore = compositeScore,
                ClueCount = 30,
                EmptyCells = 51,
                EstimatedDifficulty = difficulty,
                IsInTargetRange = matchedTarget,
                TargetDifficulty = difficulty
            }
        };
    }
}

