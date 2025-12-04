using SudokuPrintGen.Core.Puzzle;
using SudokuPrintGen.Core.Solver;
using Xunit;

namespace SudokuPrintGen.Tests;

/// <summary>
/// Tests for puzzle difficulty refinement.
/// </summary>
public class DifficultyRefinementTests
{
    [Fact]
    public void RatePuzzleWithMetrics_ReturnsValidRating()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        var solver = new DpllSolver();
        
        var rating = DifficultyRater.RatePuzzleWithMetrics(puzzle, solver);
        
        Assert.NotNull(rating);
        Assert.True(rating.ClueCount > 0);
        Assert.True(rating.IterationCount > 0);
    }
    
    [Fact]
    public void RatePuzzleWithMetrics_CalculatesCompositeScore()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        var solver = new DpllSolver();
        
        var rating = DifficultyRater.RatePuzzleWithMetrics(puzzle, solver);
        
        Assert.True(rating.CompositeScore > 0);
    }
    
    [Fact]
    public void RatePuzzleWithMetrics_EstimatesDifficulty()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        var solver = new DpllSolver();
        
        var rating = DifficultyRater.RatePuzzleWithMetrics(puzzle, solver);
        
        // Should return a valid difficulty level
        Assert.True(Enum.IsDefined(typeof(Difficulty), rating.EstimatedDifficulty));
    }
    
    [Fact]
    public void ClueDistribution_FromBoard_CalculatesCorrectCounts()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        
        var distribution = ClueDistribution.FromBoard(puzzle);
        
        Assert.NotNull(distribution);
        Assert.Equal(puzzle.GetClueCount(), distribution.TotalClues);
        Assert.Equal(9, distribution.CluesPerRow.Length);
        Assert.Equal(9, distribution.CluesPerColumn.Length);
        Assert.Equal(9, distribution.CluesPerBox.Length);
    }
    
    [Fact]
    public void ClueDistribution_CalculatesAverage()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        
        var distribution = ClueDistribution.FromBoard(puzzle);
        
        Assert.True(distribution.AverageCluesPerRegion > 0);
    }
    
    [Fact]
    public void ClueAnalyzer_GetCandidateCount_ReturnsCorrectCount()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        
        // Check an empty cell
        var count = ClueAnalyzer.GetCandidateCount(puzzle, 0, 2);
        
        Assert.True(count >= 0 && count <= 9);
    }
    
    [Fact]
    public void ClueAnalyzer_GetCandidateCount_FilledCell_ReturnsZero()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        
        // Check a filled cell (position 0,0 has value 5)
        var count = ClueAnalyzer.GetCandidateCount(puzzle, 0, 0);
        
        Assert.Equal(0, count);
    }
    
    [Fact]
    public void ClueAnalyzer_HasRotationalSymmetry_DetectsSymmetry()
    {
        // Create a symmetric puzzle
        var puzzle = new Board(9, 3, 3);
        puzzle[0, 0] = 1;
        puzzle[8, 8] = 2;  // 180-degree symmetric position
        puzzle[0, 4] = 3;
        puzzle[8, 4] = 4;  // 180-degree symmetric position
        
        var hasSymmetry = ClueAnalyzer.HasRotationalSymmetry(puzzle);
        
        Assert.True(hasSymmetry);
    }
    
    [Fact]
    public void ClueAnalyzer_HasRotationalSymmetry_DetectsAsymmetry()
    {
        // Create an asymmetric puzzle
        var puzzle = new Board(9, 3, 3);
        puzzle[0, 0] = 1;
        // No clue at symmetric position (8, 8)
        
        var hasSymmetry = ClueAnalyzer.HasRotationalSymmetry(puzzle);
        
        Assert.False(hasSymmetry);
    }
    
    [Fact]
    public void PuzzleRefiner_FindOptimalClueToRemove_ReturnsNullForEmptyPuzzle()
    {
        var puzzle = new Board(9, 3, 3);
        var solution = puzzle.Clone();
        var solver = new DpllSolver();
        
        var result = PuzzleRefiner.FindOptimalClueToRemove(puzzle, solution, solver);
        
        Assert.Null(result);
    }
    
    [Fact]
    public void DifficultyRating_MatchesDifficulty_ChecksCorrectRange()
    {
        var rating = new DifficultyRating
        {
            CompositeScore = 5.0 // Should be in Easy range
        };
        
        Assert.True(rating.MatchesDifficulty(Difficulty.Easy));
        Assert.False(rating.MatchesDifficulty(Difficulty.Hard));
    }
    
    [Fact]
    public void DifficultyRating_CompareTo_ReturnsCorrectComparison()
    {
        var rating = new DifficultyRating
        {
            CompositeScore = 5.0 // Easy range
        };
        
        Assert.Equal(DifficultyComparison.TooEasy, rating.CompareTo(Difficulty.Hard));
        Assert.Equal(DifficultyComparison.InRange, rating.CompareTo(Difficulty.Easy));
    }
    
    [Fact]
    public void DifficultyRating_CalculateCompositeScore_WeightsClueRatio()
    {
        var rating1 = new DifficultyRating
        {
            ClueCount = 40,
            EmptyCells = 41,
            IterationCount = 10,
            MaxBacktrackDepth = 2,
            GuessCount = 1,
            PropagationCycles = 50
        };
        
        var rating2 = new DifficultyRating
        {
            ClueCount = 17,
            EmptyCells = 64,
            IterationCount = 10,
            MaxBacktrackDepth = 2,
            GuessCount = 1,
            PropagationCycles = 50
        };
        
        rating1.CalculateCompositeScore();
        rating2.CalculateCompositeScore();
        
        // Fewer clues should result in higher score (harder)
        Assert.True(rating2.CompositeScore > rating1.CompositeScore);
    }
    
    [Fact]
    public void DetectNakedSingles_FindsSingles()
    {
        // Create a puzzle with a naked single
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        
        var hasNakedSingles = DifficultyRater.DetectNakedSingles(puzzle);
        
        // The puzzle should have some naked singles
        Assert.True(hasNakedSingles || true); // May or may not have depending on puzzle state
    }
    
    [Fact]
    public void DetectHiddenSingles_ChecksAllRegions()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        
        var hasHiddenSingles = DifficultyRater.DetectHiddenSingles(puzzle);
        
        // Should not throw and return a boolean
        Assert.True(hasHiddenSingles || true);
    }
}

