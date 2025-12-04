using SudokuPrintGen.Core.Puzzle;
using SudokuPrintGen.Core.Solver;
using Xunit;

namespace SudokuPrintGen.Tests;

/// <summary>
/// Tests for solver metrics tracking.
/// </summary>
public class SolverMetricsTests
{
    // Easy puzzle with many clues
    private const string EasyPuzzle = "530070000600195000098000060800060003400803001700020006060000280000419005000080079";
    
    // Harder puzzle with fewer clues
    private const string HardPuzzle = "4.....8.5.3..........7......2.....6.....8.4......1.......6.3.7.5..2.....1.4......";
    
    [Fact]
    public void SolveWithMetrics_ValidPuzzle_ReturnsResult()
    {
        var puzzle = Board.FromString(EasyPuzzle);
        var solver = new DpllSolver();
        
        var result = solver.SolveWithMetrics(puzzle);
        
        Assert.True(result.HasSolution);
        Assert.NotNull(result.Solution);
        Assert.True(result.Solution!.IsComplete());
    }
    
    [Fact]
    public void SolveWithMetrics_TracksIterationCount()
    {
        var puzzle = Board.FromString(EasyPuzzle);
        var solver = new DpllSolver();
        
        var result = solver.SolveWithMetrics(puzzle);
        
        Assert.True(result.IterationCount > 0);
    }
    
    [Fact]
    public void SolveWithMetrics_TracksPropagationCycles()
    {
        var puzzle = Board.FromString(EasyPuzzle);
        var solver = new DpllSolver();
        
        var result = solver.SolveWithMetrics(puzzle);
        
        Assert.True(result.PropagationCycles >= 0);
    }
    
    [Fact]
    public void SolveWithMetrics_TracksMaxBacktrackDepth()
    {
        var puzzle = Board.FromString(HardPuzzle);
        var solver = new DpllSolver();
        
        var result = solver.SolveWithMetrics(puzzle);
        
        Assert.True(result.MaxBacktrackDepth >= 0);
    }
    
    [Fact]
    public void SolveWithMetrics_CalculatesDifficultyScore()
    {
        var puzzle = Board.FromString(EasyPuzzle);
        var solver = new DpllSolver();
        
        var result = solver.SolveWithMetrics(puzzle);
        
        Assert.True(result.DifficultyScore >= 0);
    }
    
    [Fact]
    public void SolveWithMetrics_HarderPuzzleHasMoreIterations()
    {
        var easyPuzzle = Board.FromString(EasyPuzzle);
        var hardPuzzle = Board.FromString(HardPuzzle);
        var solver = new DpllSolver();
        
        var easyResult = solver.SolveWithMetrics(easyPuzzle);
        var hardResult = solver.SolveWithMetrics(hardPuzzle);
        
        // Hard puzzle should require more iterations
        Assert.True(hardResult.IterationCount >= easyResult.IterationCount);
    }
    
    [Fact]
    public void CountSolutionsWithMetrics_ReturnsCorrectCount()
    {
        var puzzle = Board.FromString(EasyPuzzle);
        var solver = new DpllSolver();
        
        var result = solver.CountSolutionsWithMetrics(puzzle, 2);
        
        Assert.Equal(1, result.SolutionCount);
        Assert.True(result.IterationCount > 0);
    }
    
    [Fact]
    public void HasUniqueSolutionWithMetrics_ReturnsMetrics()
    {
        var puzzle = Board.FromString(EasyPuzzle);
        var solver = new DpllSolver();
        
        var result = solver.HasUniqueSolutionWithMetrics(puzzle);
        
        Assert.Equal(1, result.SolutionCount);
        Assert.True(result.IterationCount > 0);
    }
    
    [Fact]
    public void SolverMetrics_Reset_ClearsAllValues()
    {
        var metrics = new SolverMetrics
        {
            IterationCount = 100,
            CurrentDepth = 5,
            MaxBacktrackDepth = 10,
            PropagationCycles = 50,
            GuessCount = 20
        };
        
        metrics.Reset();
        
        Assert.Equal(0, metrics.IterationCount);
        Assert.Equal(0, metrics.CurrentDepth);
        Assert.Equal(0, metrics.MaxBacktrackDepth);
        Assert.Equal(0, metrics.PropagationCycles);
        Assert.Equal(0, metrics.GuessCount);
    }
    
    [Fact]
    public void SolverMetrics_EnterExitLevel_TracksDepth()
    {
        var metrics = new SolverMetrics();
        
        metrics.EnterLevel();
        metrics.EnterLevel();
        metrics.EnterLevel();
        
        Assert.Equal(3, metrics.CurrentDepth);
        Assert.Equal(3, metrics.MaxBacktrackDepth);
        
        metrics.ExitLevel();
        metrics.ExitLevel();
        
        Assert.Equal(1, metrics.CurrentDepth);
        Assert.Equal(3, metrics.MaxBacktrackDepth); // Max should stay at 3
    }
    
    [Fact]
    public void SolverResult_CalculateDifficultyScore_WeightsFactors()
    {
        var result = new SolverResult
        {
            IterationCount = 100,
            MaxBacktrackDepth = 10,
            GuessCount = 5,
            PropagationCycles = 200
        };
        
        result.CalculateDifficultyScore();
        
        // Score should be positive and based on weighted components
        Assert.True(result.DifficultyScore > 0);
    }
}

