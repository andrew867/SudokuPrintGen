using SudokuPrintGen.Core.Puzzle;

namespace SudokuPrintGen.Core.Solver;

/// <summary>
/// Contains the result of a solve operation including performance metrics.
/// </summary>
public class SolverResult
{
    /// <summary>
    /// The solved board, or null if no solution was found.
    /// </summary>
    public Board? Solution { get; set; }
    
    /// <summary>
    /// Whether a solution was found.
    /// </summary>
    public bool HasSolution => Solution != null;
    
    /// <summary>
    /// Number of solutions found (for counting operations).
    /// </summary>
    public int SolutionCount { get; set; }
    
    /// <summary>
    /// Number of recursive calls/backtracking steps during solving.
    /// This is the primary difficulty metric.
    /// </summary>
    public int IterationCount { get; set; }
    
    /// <summary>
    /// Maximum depth of the backtracking tree reached during solving.
    /// </summary>
    public int MaxBacktrackDepth { get; set; }
    
    /// <summary>
    /// Number of constraint propagation cycles performed.
    /// </summary>
    public int PropagationCycles { get; set; }
    
    /// <summary>
    /// Number of guesses made (cells where multiple candidates were tried).
    /// </summary>
    public int GuessCount { get; set; }
    
    /// <summary>
    /// Composite difficulty score calculated from all metrics.
    /// Higher values indicate harder puzzles.
    /// </summary>
    public double DifficultyScore { get; set; }
    
    /// <summary>
    /// Calculates and sets the composite difficulty score based on metrics.
    /// </summary>
    public void CalculateDifficultyScore()
    {
        // Weighted formula:
        // - Iteration count: 50% (primary metric)
        // - Max backtrack depth: 20%
        // - Guess count: 20%
        // - Propagation cycles: 10% (normalized to similar scale)
        
        double iterationComponent = IterationCount * 0.50;
        double depthComponent = MaxBacktrackDepth * 2.0 * 0.20; // Scale depth up
        double guessComponent = GuessCount * 3.0 * 0.20; // Scale guesses up
        double propagationComponent = (PropagationCycles / 10.0) * 0.10; // Scale down
        
        DifficultyScore = iterationComponent + depthComponent + guessComponent + propagationComponent;
    }
    
    /// <summary>
    /// Creates a result indicating no solution was found.
    /// </summary>
    public static SolverResult NoSolution(SolverMetrics metrics)
    {
        return new SolverResult
        {
            Solution = null,
            SolutionCount = 0,
            IterationCount = metrics.IterationCount,
            MaxBacktrackDepth = metrics.MaxBacktrackDepth,
            PropagationCycles = metrics.PropagationCycles,
            GuessCount = metrics.GuessCount
        };
    }
    
    /// <summary>
    /// Creates a result with a solution.
    /// </summary>
    public static SolverResult WithSolution(Board solution, SolverMetrics metrics)
    {
        var result = new SolverResult
        {
            Solution = solution,
            SolutionCount = 1,
            IterationCount = metrics.IterationCount,
            MaxBacktrackDepth = metrics.MaxBacktrackDepth,
            PropagationCycles = metrics.PropagationCycles,
            GuessCount = metrics.GuessCount
        };
        result.CalculateDifficultyScore();
        return result;
    }
    
    /// <summary>
    /// Creates a result for solution counting.
    /// </summary>
    public static SolverResult ForCounting(int solutionCount, Board? firstSolution, SolverMetrics metrics)
    {
        var result = new SolverResult
        {
            Solution = firstSolution,
            SolutionCount = solutionCount,
            IterationCount = metrics.IterationCount,
            MaxBacktrackDepth = metrics.MaxBacktrackDepth,
            PropagationCycles = metrics.PropagationCycles,
            GuessCount = metrics.GuessCount
        };
        result.CalculateDifficultyScore();
        return result;
    }
}

/// <summary>
/// Mutable metrics container used during solving.
/// </summary>
public class SolverMetrics
{
    /// <summary>
    /// Number of recursive calls/iterations.
    /// </summary>
    public int IterationCount { get; set; }
    
    /// <summary>
    /// Current depth in the backtracking tree.
    /// </summary>
    public int CurrentDepth { get; set; }
    
    /// <summary>
    /// Maximum depth reached during solving.
    /// </summary>
    public int MaxBacktrackDepth { get; set; }
    
    /// <summary>
    /// Number of constraint propagation cycles.
    /// </summary>
    public int PropagationCycles { get; set; }
    
    /// <summary>
    /// Number of guesses (branching decisions) made.
    /// </summary>
    public int GuessCount { get; set; }
    
    /// <summary>
    /// Increments iteration count and updates max depth.
    /// </summary>
    public void IncrementIteration()
    {
        IterationCount++;
        if (CurrentDepth > MaxBacktrackDepth)
        {
            MaxBacktrackDepth = CurrentDepth;
        }
    }
    
    /// <summary>
    /// Records entering a new level of recursion.
    /// </summary>
    public void EnterLevel()
    {
        CurrentDepth++;
        if (CurrentDepth > MaxBacktrackDepth)
        {
            MaxBacktrackDepth = CurrentDepth;
        }
    }
    
    /// <summary>
    /// Records exiting a level of recursion.
    /// </summary>
    public void ExitLevel()
    {
        CurrentDepth--;
    }
    
    /// <summary>
    /// Records a guess (trying multiple candidates).
    /// </summary>
    public void RecordGuess()
    {
        GuessCount++;
    }
    
    /// <summary>
    /// Records a constraint propagation cycle.
    /// </summary>
    public void RecordPropagation()
    {
        PropagationCycles++;
    }
    
    /// <summary>
    /// Resets all metrics to zero.
    /// </summary>
    public void Reset()
    {
        IterationCount = 0;
        CurrentDepth = 0;
        MaxBacktrackDepth = 0;
        PropagationCycles = 0;
        GuessCount = 0;
    }
}

