using SudokuPrintGen.Core.Puzzle;

namespace SudokuPrintGen.Core.Output;

/// <summary>
/// Statistics about puzzle generation and solving.
/// </summary>
public class OutputStatistics
{
    /// <summary>
    /// Time taken to generate the puzzle.
    /// </summary>
    public TimeSpan GenerationTime { get; set; }
    
    /// <summary>
    /// Time taken to solve/verify the puzzle.
    /// </summary>
    public TimeSpan SolveTime { get; set; }
    
    /// <summary>
    /// Number of guesses made during solving.
    /// </summary>
    public int GuessCount { get; set; }
    
    /// <summary>
    /// Number of clues in the puzzle.
    /// </summary>
    public int ClueCount { get; set; }
    
    /// <summary>
    /// Number of generation attempts before success.
    /// </summary>
    public int Attempts { get; set; }
    
    /// <summary>
    /// Target difficulty level requested.
    /// </summary>
    public Difficulty TargetDifficulty { get; set; }
    
    /// <summary>
    /// Actual difficulty rating of the generated puzzle.
    /// </summary>
    public DifficultyRating? ActualDifficultyRating { get; set; }
    
    /// <summary>
    /// Whether the actual difficulty matches the target.
    /// </summary>
    public bool DifficultyMatch { get; set; }
    
    /// <summary>
    /// Number of refinement iterations used (for iterative generation).
    /// </summary>
    public int RefinementIterations { get; set; }
    
    /// <summary>
    /// Solver iteration count before refinement.
    /// </summary>
    public int InitialIterationCount { get; set; }
    
    /// <summary>
    /// Solver iteration count after refinement.
    /// </summary>
    public int FinalIterationCount { get; set; }
}
