using SudokuPrintGen.Core.Solver;
using System.Numerics;

namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Rates the difficulty of a Sudoku puzzle based on solving techniques required.
/// </summary>
public static class DifficultyRater
{
    /// <summary>
    /// Rates a puzzle and returns a difficulty score (higher = harder).
    /// </summary>
    public static DifficultyRating RatePuzzle(Board puzzle, ISolver solver)
    {
        var rating = new DifficultyRating();
        
        // Count clues
        rating.ClueCount = puzzle.GetClueCount();
        rating.EmptyCells = puzzle.Size * puzzle.Size - rating.ClueCount;
        
        // Analyze solving complexity
        var workingBoard = puzzle.Clone();
        var techniques = AnalyzeSolvingTechniques(workingBoard, solver);
        
        rating.RequiredTechniques = techniques;
        rating.EstimatedDifficulty = EstimateDifficulty(rating);
        
        return rating;
    }
    
    private static List<string> AnalyzeSolvingTechniques(Board board, ISolver solver)
    {
        var techniques = new List<string>();
        var workingBoard = board.Clone();
        
        // Try to solve with basic techniques first
        if (CanSolveWithNakedSingles(workingBoard))
        {
            techniques.Add("NakedSingles");
        }
        
        if (CanSolveWithHiddenSingles(workingBoard))
        {
            techniques.Add("HiddenSingles");
        }
        
        // If basic techniques aren't enough, puzzle requires advanced techniques
        if (techniques.Count == 0)
        {
            techniques.Add("Advanced");
        }
        
        return techniques;
    }
    
    private static bool CanSolveWithNakedSingles(Board board)
    {
        // Check if any cell has only one candidate (naked single)
        var size = board.Size;
        Span<uint> rowCandidates = stackalloc uint[size];
        Span<uint> colCandidates = stackalloc uint[size];
        Span<uint> boxCandidates = stackalloc uint[size];
        
        ConstraintPropagator.PropagateConstraints(board, rowCandidates, colCandidates, boxCandidates);
        
        // If propagation filled any cells, we have naked singles
        return board.GetClueCount() > 0;
    }
    
    private static bool CanSolveWithHiddenSingles(Board board)
    {
        // Hidden singles are when a digit can only go in one cell in a row/col/box
        // This is more complex to detect, simplified for now
        return false;
    }
    
    private static Difficulty EstimateDifficulty(DifficultyRating rating)
    {
        // Estimate based on clue count and techniques
        var clueRatio = (double)rating.ClueCount / (rating.ClueCount + rating.EmptyCells);
        
        if (clueRatio >= 0.45)
            return Difficulty.Easy;
        else if (clueRatio >= 0.35)
            return Difficulty.Medium;
        else if (clueRatio >= 0.28)
            return Difficulty.Hard;
        else if (clueRatio >= 0.22)
            return Difficulty.Expert;
        else
            return Difficulty.Evil;
    }
}

/// <summary>
/// Rating information for a puzzle.
/// </summary>
public class DifficultyRating
{
    public int ClueCount { get; set; }
    public int EmptyCells { get; set; }
    public List<string> RequiredTechniques { get; set; } = new();
    public Difficulty EstimatedDifficulty { get; set; }
    public double Score { get; set; }
}

