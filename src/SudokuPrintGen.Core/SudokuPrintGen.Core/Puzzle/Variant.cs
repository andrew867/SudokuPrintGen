namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Represents different Sudoku puzzle variants.
/// </summary>
public enum Variant
{
    /// <summary>
    /// Classic Sudoku: standard 9x9 grid with 3x3 boxes.
    /// </summary>
    Classic,
    
    /// <summary>
    /// Diagonal Sudoku: both main diagonals must also contain digits 1-9.
    /// </summary>
    Diagonal,
    
    /// <summary>
    /// Color-constrained Sudoku: 9 colors, same position in each quadrant must contain 1-9.
    /// </summary>
    ColorConstrained,
    
    /// <summary>
    /// Kikagaku: irregular colored shapes instead of 3x3 blocks.
    /// </summary>
    Kikagaku
}

