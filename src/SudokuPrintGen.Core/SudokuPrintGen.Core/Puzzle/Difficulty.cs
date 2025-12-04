namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Represents the difficulty level of a Sudoku puzzle.
/// </summary>
public enum Difficulty
{
    Easy,    // ~40 clues for 9x9
    Medium,  // ~32 clues for 9x9
    Hard,    // ~26 clues for 9x9
    Expert,  // ~20 clues for 9x9
    Evil     // ~17 clues for 9x9
}

/// <summary>
/// Extension methods for Difficulty enum.
/// </summary>
public static class DifficultyExtensions
{
    /// <summary>
    /// Gets the target number of clues for a given difficulty and board size.
    /// </summary>
    public static int GetTargetClues(this Difficulty difficulty, int boardSize)
    {
        var totalCells = boardSize * boardSize;
        var percentage = difficulty switch
        {
            Difficulty.Easy => 0.49,   // ~49% filled
            Difficulty.Medium => 0.39, // ~39% filled
            Difficulty.Hard => 0.32,   // ~32% filled
            Difficulty.Expert => 0.25, // ~25% filled
            Difficulty.Evil => 0.21,  // ~21% filled (minimum is 17 for 9x9)
            _ => 0.39
        };
        
        var clues = (int)(totalCells * percentage);
        
        // For 9x9, ensure minimum of 17 clues (mathematical minimum for unique solution)
        if (boardSize == 9 && clues < 17)
        {
            clues = 17;
        }
        
        return clues;
    }
}

/// <summary>
/// Provides iteration-based difficulty targets for puzzle generation.
/// Based on research from "Rating and Generating Sudoku Puzzles Based On
/// Constraint Satisfaction Problems" and empirical testing.
/// </summary>
public static class DifficultyTargets
{
    /// <summary>
    /// Target iteration counts for each difficulty level.
    /// These represent the "goal" number of solver iterations.
    /// </summary>
    private static readonly Dictionary<Difficulty, int> IterationGoals = new()
    {
        { Difficulty.Easy, 5 },
        { Difficulty.Medium, 15 },
        { Difficulty.Hard, 40 },
        { Difficulty.Expert, 150 },
        { Difficulty.Evil, 400 }
    };
    
    /// <summary>
    /// Iteration ranges (min, max) for each difficulty level.
    /// Puzzles within these ranges are considered to match the difficulty.
    /// </summary>
    private static readonly Dictionary<Difficulty, (int min, int max)> IterationRanges = new()
    {
        { Difficulty.Easy, (1, 10) },
        { Difficulty.Medium, (11, 25) },
        { Difficulty.Hard, (26, 80) },
        { Difficulty.Expert, (81, 350) },
        { Difficulty.Evil, (351, int.MaxValue) }
    };
    
    /// <summary>
    /// Difficulty score ranges for composite scoring.
    /// </summary>
    private static readonly Dictionary<Difficulty, (double min, double max)> ScoreRanges = new()
    {
        { Difficulty.Easy, (0, 8) },
        { Difficulty.Medium, (8, 20) },
        { Difficulty.Hard, (20, 60) },
        { Difficulty.Expert, (60, 250) },
        { Difficulty.Evil, (250, double.MaxValue) }
    };
    
    /// <summary>
    /// Gets the target iteration count for a difficulty level.
    /// </summary>
    public static int GetIterationGoal(Difficulty difficulty)
    {
        return IterationGoals.TryGetValue(difficulty, out var goal) ? goal : 15;
    }
    
    /// <summary>
    /// Gets the acceptable iteration range for a difficulty level.
    /// </summary>
    public static (int min, int max) GetIterationRange(Difficulty difficulty)
    {
        return IterationRanges.TryGetValue(difficulty, out var range) ? range : (11, 25);
    }
    
    /// <summary>
    /// Gets the difficulty score range for a difficulty level.
    /// </summary>
    public static (double min, double max) GetScoreRange(Difficulty difficulty)
    {
        return ScoreRanges.TryGetValue(difficulty, out var range) ? range : (8, 20);
    }
    
    /// <summary>
    /// Checks if an iteration count falls within the range for a difficulty level.
    /// </summary>
    public static bool IsInRange(int iterationCount, Difficulty difficulty)
    {
        var (min, max) = GetIterationRange(difficulty);
        return iterationCount >= min && iterationCount <= max;
    }
    
    /// <summary>
    /// Checks if a difficulty score falls within the range for a difficulty level.
    /// </summary>
    public static bool IsScoreInRange(double score, Difficulty difficulty)
    {
        var (min, max) = GetScoreRange(difficulty);
        return score >= min && score < max;
    }
    
    /// <summary>
    /// Maps an iteration count to the corresponding difficulty level.
    /// </summary>
    public static Difficulty GetDifficultyFromIterations(int iterationCount)
    {
        if (iterationCount <= 10) return Difficulty.Easy;
        if (iterationCount <= 25) return Difficulty.Medium;
        if (iterationCount <= 80) return Difficulty.Hard;
        if (iterationCount <= 350) return Difficulty.Expert;
        return Difficulty.Evil;
    }
    
    /// <summary>
    /// Maps a difficulty score to the corresponding difficulty level.
    /// </summary>
    public static Difficulty GetDifficultyFromScore(double score)
    {
        if (score < 8) return Difficulty.Easy;
        if (score < 20) return Difficulty.Medium;
        if (score < 60) return Difficulty.Hard;
        if (score < 250) return Difficulty.Expert;
        return Difficulty.Evil;
    }
    
    /// <summary>
    /// Calculates the relative deviation from the target for a difficulty level.
    /// Returns a value between 0 (perfect match) and 1+ (far from target).
    /// </summary>
    public static double GetRelativeDeviation(int iterationCount, Difficulty difficulty)
    {
        var goal = GetIterationGoal(difficulty);
        if (goal == 0) return 1.0;
        return Math.Abs((double)iterationCount / goal - 1.0);
    }
    
    /// <summary>
    /// Determines if the iteration count is close enough to the target.
    /// Uses both relative and absolute tolerances.
    /// </summary>
    public static bool IsCloseToTarget(int iterationCount, Difficulty difficulty, double relativeTolerance = 0.5, int absoluteTolerance = 5)
    {
        var goal = GetIterationGoal(difficulty);
        var absoluteDiff = Math.Abs(iterationCount - goal);
        var relativeDiff = GetRelativeDeviation(iterationCount, difficulty);
        
        // Accept if within relative tolerance OR within absolute tolerance
        return relativeDiff <= relativeTolerance || absoluteDiff <= absoluteTolerance;
    }
    
    /// <summary>
    /// Compares the current iteration count to the target and returns whether
    /// the puzzle is too easy, too hard, or in range.
    /// </summary>
    public static DifficultyComparison CompareToDifficulty(int iterationCount, Difficulty targetDifficulty)
    {
        var (min, max) = GetIterationRange(targetDifficulty);
        
        if (iterationCount < min)
            return DifficultyComparison.TooEasy;
        if (iterationCount > max)
            return DifficultyComparison.TooHard;
        return DifficultyComparison.InRange;
    }
    
    /// <summary>
    /// Compares the difficulty score to the target range.
    /// </summary>
    public static DifficultyComparison CompareScoreToDifficulty(double score, Difficulty targetDifficulty)
    {
        var (min, max) = GetScoreRange(targetDifficulty);
        
        if (score < min)
            return DifficultyComparison.TooEasy;
        if (score >= max)
            return DifficultyComparison.TooHard;
        return DifficultyComparison.InRange;
    }
}

/// <summary>
/// Result of comparing a puzzle's difficulty to a target.
/// </summary>
public enum DifficultyComparison
{
    /// <summary>Puzzle is easier than the target range.</summary>
    TooEasy,
    
    /// <summary>Puzzle is within the target difficulty range.</summary>
    InRange,
    
    /// <summary>Puzzle is harder than the target range.</summary>
    TooHard
}

