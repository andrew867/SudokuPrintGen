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

