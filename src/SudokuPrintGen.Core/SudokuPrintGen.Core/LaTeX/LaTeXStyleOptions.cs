namespace SudokuPrintGen.Core.LaTeX;

/// <summary>
/// Options for LaTeX generation styling.
/// </summary>
public class LaTeXStyleOptions
{
    public LaTeXEngine Engine { get; set; } = LaTeXEngine.XeLaTeX;
    public string FontSize { get; set; } = "12pt";
    
    /// <summary>
    /// Font family name (for installed system fonts) or null to use default bundled font.
    /// </summary>
    public string? FontFamily { get; set; }
    
    /// <summary>
    /// Path to a custom TTF font file, or null to use the default bundled Futura Bold BT.
    /// </summary>
    public string? FontPath { get; set; }
    
    /// <summary>
    /// Whether to use the bundled Futura Bold BT font. Default is true.
    /// Set to false if using a system-installed font via FontFamily.
    /// </summary>
    public bool UseBundledFont { get; set; } = true;
    
    public string CellWidth { get; set; } = "4ex"; // 25% larger (was 3ex)
    public string Title { get; set; } = "Sudoku Puzzle";
    public string Author { get; set; } = "SudokuPrintGen";
    public bool IncludeSolution { get; set; } = false;
    public bool IncludeSolvingSheet { get; set; } = false;
    /// <summary>
    /// Number of puzzles per page for multi-puzzle documents.
    /// Supported values: 6 (2x3, larger puzzles) or 8 (2x4, more puzzles).
    /// Default is 6 for ~29% larger puzzles than 8-per-page layout.
    /// </summary>
    public int PuzzlesPerPage { get; set; } = 6;
}

/// <summary>
/// LaTeX engine types.
/// </summary>
public enum LaTeXEngine
{
    PdfLaTeX,
    XeLaTeX
}

