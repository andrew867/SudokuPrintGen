using SudokuPrintGen.Core.Puzzle;
using System.Text;
using System.Linq;

namespace SudokuPrintGen.Core.LaTeX;

/// <summary>
/// Generates LaTeX code for Sudoku puzzles using TikZ for beautiful, precise grids.
/// </summary>
public class LaTeXGenerator
{
    private readonly LaTeXStyleOptions _options;
    
    // TikZ grid dimensions (in cm for precision)
    // Letter paper usable area with 0.2in top/bottom, 0.25in left/right: ~20.3cm x 26.9cm
    // 6 puzzles per page (2x3): cells 0.90cm = 9mm, grid 8.1cm (~29% larger than 8-per-page)
    // 8 puzzles per page (2x4): cells 0.70cm = 7mm, grid 6.3cm
    private const double CellSizeCm6PerPage = 0.90;  // For 2x3 layout
    private const double CellSizeCm8PerPage = 0.70;  // For 2x4 layout
    private const double ThinLineWidth = 0.3; // pt
    private const double ThickLineWidth = 1.2; // pt
    
    /// <summary>
    /// Gets the cell size based on puzzles per page setting.
    /// </summary>
    private double GetCellSizeCm() => _options.PuzzlesPerPage <= 6 ? CellSizeCm6PerPage : CellSizeCm8PerPage;
    
    public LaTeXGenerator(LaTeXStyleOptions? options = null)
    {
        _options = options ?? new LaTeXStyleOptions();
    }
    
    /// <summary>
    /// Generates LaTeX code for a single puzzle document.
    /// </summary>
    public string Generate(Board puzzle, Board? solution = null, GeneratedPuzzle? generatedPuzzle = null)
    {
        var sb = new StringBuilder();
        
        // Document header with TikZ
        sb.Append(GenerateDocumentHeader(generatedPuzzle));
        
        // Puzzle grid
        sb.AppendLine(@"\begin{center}");
        sb.Append(GenerateTikZGrid(puzzle, generatedPuzzle, 1.0)); // Full size for single puzzle
        sb.AppendLine(@"\end{center}");
        
        // Solution if provided
        if (solution != null && _options.IncludeSolution)
        {
            sb.AppendLine(@"\newpage");
            sb.AppendLine(@"\begin{center}");
            sb.AppendLine(@"{\large\textbf{Solution}}");
            sb.AppendLine(@"\vspace{0.5cm}");
            sb.Append(GenerateTikZGrid(solution, generatedPuzzle, 1.0));
            sb.AppendLine(@"\end{center}");
        }
        
        // Add solving sheet if requested
        if (_options.IncludeSolvingSheet)
        {
            sb.AppendLine(@"\newpage");
            sb.AppendLine(@"\begin{center}");
            sb.AppendLine(@"{\large\textbf{Solving Sheet}}");
            sb.AppendLine(@"\vspace{0.5cm}");
            sb.Append(GenerateEmptyTikZGrid(puzzle.Size, 1.0));
            sb.AppendLine(@"\end{center}");
        }
        
        // Add metadata footer
        if (generatedPuzzle != null)
        {
            sb.AppendLine(@"\vspace{1cm}");
            sb.AppendLine(@"\begin{center}");
            sb.AppendLine(@"\small");
            var metadata = new List<string> { $@"Difficulty: {generatedPuzzle.Difficulty}" };
            if (generatedPuzzle.Seed.HasValue)
            {
                metadata.Add($@"Seed: {generatedPuzzle.Seed.Value}");
            }
            metadata.Add($@"Puzzle \#{generatedPuzzle.PuzzleNumber}");
            metadata.Add($@"Generated: {generatedPuzzle.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
            metadata.Add($@"Solver: {generatedPuzzle.SolverAlgorithm}");
            
            if (generatedPuzzle.DifficultyRating != null)
            {
                metadata.Add($@"Clues: {generatedPuzzle.DifficultyRating.ClueCount}");
            }
            
            if (generatedPuzzle.Symmetry != null && generatedPuzzle.Symmetry.SymmetryScore > 0)
            {
                var symTypes = generatedPuzzle.Symmetry.GetSymmetryTypes();
                if (symTypes.Count > 0)
                {
                    metadata.Add($@"Symmetry: {string.Join(", ", symTypes)}");
                }
            }
            
            sb.AppendLine(string.Join(" | ", metadata));
            sb.AppendLine(@"\end{center}");
        }
        
        sb.AppendLine(@"\end{document}");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates LaTeX code for multiple puzzles in one document.
    /// Layout: 2 columns x N rows (6 per page = 2x3, 8 per page = 2x4).
    /// </summary>
    public string GenerateMultiplePuzzles(List<(Board puzzle, Board? solution, GeneratedPuzzle? metadata)> puzzles)
    {
        var sb = new StringBuilder();
        int maxPuzzlesPerPage = _options.PuzzlesPerPage <= 6 ? 6 : 8;
        const int puzzlesPerRow = 2;
        
        // Document header with TikZ
        var firstPuzzle = puzzles.FirstOrDefault();
        sb.Append(GenerateDocumentHeader(firstPuzzle.metadata));
        
        // Balance puzzles across pages
        var pageDistribution = BalancePuzzlesAcrossPages(puzzles.Count, maxPuzzlesPerPage);
        
        int puzzleIndex = 0;
        int pageNumber = 0;
        while (puzzleIndex < puzzles.Count)
        {
            if (pageNumber > 0)
            {
                sb.AppendLine(@"\newpage");
            }
            
            int puzzlesOnThisPage = pageDistribution[pageNumber];
            int rowsNeeded = (int)Math.Ceiling((double)puzzlesOnThisPage / puzzlesPerRow);
            
            // Generate puzzles in a 2-column layout, centered on page
            for (int row = 0; row < rowsNeeded; row++)
            {
                int leftIdx = puzzleIndex + (row * puzzlesPerRow);
                int rightIdx = puzzleIndex + (row * puzzlesPerRow) + 1;
                
                bool hasLeft = leftIdx < puzzles.Count && leftIdx < puzzleIndex + puzzlesOnThisPage;
                bool hasRight = rightIdx < puzzles.Count && rightIdx < puzzleIndex + puzzlesOnThisPage;
                
                // Center the row of puzzles using \centering (no extra vertical space like \begin{center})
                sb.AppendLine(@"\noindent\hfill");
                sb.AppendLine(@"\begin{minipage}[t]{0.48\textwidth}");
                sb.AppendLine(@"\centering");
                if (hasLeft)
                {
                    var (puzzle, _, metadata) = puzzles[leftIdx];
                    sb.Append(GenerateTikZGridWithFooter(puzzle, metadata));
                }
                sb.AppendLine(@"\end{minipage}");
                sb.AppendLine(@"\hfill");
                sb.AppendLine(@"\begin{minipage}[t]{0.48\textwidth}");
                sb.AppendLine(@"\centering");
                if (hasRight)
                {
                    var (puzzle, _, metadata) = puzzles[rightIdx];
                    sb.Append(GenerateTikZGridWithFooter(puzzle, metadata));
                }
                sb.AppendLine(@"\end{minipage}");
                sb.AppendLine(@"\hfill\par");
                
                // Minimal vertical spacing between rows (but not after last row)
                if (row < rowsNeeded - 1)
                {
                    sb.AppendLine(@"\vspace{0.5cm}");
                }
            }
            
            puzzleIndex += puzzlesOnThisPage;
            pageNumber++;
        }
        
        sb.AppendLine(@"\end{document}");
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates the document header with TikZ package.
    /// </summary>
    private string GenerateDocumentHeader(GeneratedPuzzle? metadata)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"\documentclass[11pt]{article}");
        sb.AppendLine(@"\usepackage[utf8]{inputenc}");
        sb.AppendLine(@"\usepackage{xcolor}");
        sb.AppendLine(@"\usepackage{tikz}");
        sb.AppendLine(@"\usepackage{geometry}");
        
        // Minimal margins for letter paper - maximizes puzzle size for 8 per page
        // 0.2in is near the printable limit for most printers
        sb.AppendLine(@"\geometry{letterpaper,");
        sb.AppendLine(@"          left=0.25in, right=0.25in,");
        sb.AppendLine(@"          top=0.2in, bottom=0.2in}");
        
        // Eliminate default paragraph spacing
        sb.AppendLine(@"\setlength{\parskip}{0pt}");
        sb.AppendLine(@"\setlength{\parsep}{0pt}");
        
        // XeLaTeX font support
        if (_options.Engine == LaTeXEngine.XeLaTeX)
        {
            sb.AppendLine(@"\usepackage{fontspec}");
            
            // Font configuration: custom font path, installed font, or bundled default
            if (!string.IsNullOrEmpty(_options.FontPath))
            {
                // Custom font file path specified
                var fontPath = Path.GetFullPath(_options.FontPath);
                var fontDir = Path.GetDirectoryName(fontPath) ?? ".";
                var fontFile = Path.GetFileNameWithoutExtension(fontPath);
                var fontExt = Path.GetExtension(fontPath);
                // Use forward slashes for LaTeX path compatibility
                var latexFontDir = fontDir.Replace('\\', '/') + "/";
                sb.AppendLine($@"\setmainfont{{{fontFile}}}[Path={{{latexFontDir}}}, Extension={fontExt}]");
            }
            else if (!string.IsNullOrEmpty(_options.FontFamily))
            {
                // Installed system font by name
                sb.AppendLine(@"\setmainfont{" + _options.FontFamily + "}");
            }
            else if (_options.UseBundledFont)
            {
                // Use bundled Futura Bold BT font
                var bundledFontPath = FindBundledFontPath();
                if (bundledFontPath != null)
                {
                    var fontDir = Path.GetDirectoryName(bundledFontPath) ?? ".";
                    // Use forward slashes for LaTeX path compatibility
                    var latexFontDir = fontDir.Replace('\\', '/') + "/";
                    sb.AppendLine($@"\setmainfont{{Futura Bold BT}}[Path={{{latexFontDir}}}, Extension=.ttf]");
                }
            }
        }
        
        // Color definitions for variants
        if (metadata?.Variant == Variant.ColorConstrained || metadata?.Variant == Variant.Kikagaku)
        {
            sb.AppendLine(@"\definecolor{sred}{HTML}{FAB3BA}");
            sb.AppendLine(@"\definecolor{sviolet}{HTML}{EDD4FF}");
            sb.AppendLine(@"\definecolor{sgrey}{HTML}{DFDDD8}");
            sb.AppendLine(@"\definecolor{sorange}{HTML}{F3CE82}");
            sb.AppendLine(@"\definecolor{spink}{HTML}{F1A1DC}");
            sb.AppendLine(@"\definecolor{syellow}{HTML}{F6FC7B}");
            sb.AppendLine(@"\definecolor{slgreen}{HTML}{C8FBAE}");
            sb.AppendLine(@"\definecolor{sdgreen}{HTML}{99EECD}");
            sb.AppendLine(@"\definecolor{sblue}{HTML}{A4DFF2}");
        }
        else if (metadata?.Variant == Variant.Diagonal)
        {
            sb.AppendLine(@"\definecolor{diagcolor}{HTML}{FFFACD}"); // Light yellow for diagonals
        }
        
        sb.AppendLine(@"\begin{document}");
        sb.AppendLine(@"\pagestyle{empty}");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Finds the bundled Futura Bold BT font file.
    /// Priority: exe directory first (where font is copied during build).
    /// </summary>
    private static string? FindBundledFontPath()
    {
        const string fontFileName = "Futura Bold BT.ttf";
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        
        var possiblePaths = new[]
        {
            // Priority 1: fonts folder next to executable (copied during build)
            Path.Combine(exeDir, "fonts", fontFileName),
            // Priority 2: directly next to executable
            Path.Combine(exeDir, fontFileName),
            // Priority 3: relative to project root (for development without build)
            Path.Combine(exeDir, "..", "..", "..", "..", "..", "fonts", fontFileName),
            // Priority 4: from current working directory
            Path.Combine(Directory.GetCurrentDirectory(), "fonts", fontFileName),
        };
        
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Generates a TikZ-based Sudoku grid with the puzzle numbers.
    /// </summary>
    private string GenerateTikZGrid(Board puzzle, GeneratedPuzzle? metadata, double scale)
    {
        var sb = new StringBuilder();
        double cellSize = GetCellSizeCm() * scale;
        double gridSize = cellSize * 9;
        
        sb.AppendLine($@"\begin{{tikzpicture}}[scale=1]");
        
        // Draw cell backgrounds for variants
        if (metadata?.Variant == Variant.Diagonal)
        {
            // Fill diagonal cells
            for (int i = 0; i < 9; i++)
            {
                double x1 = i * cellSize;
                double y1 = (8 - i) * cellSize;
                double x2 = (8 - i) * cellSize;
                double y2 = i * cellSize;
                sb.AppendLine($@"  \fill[diagcolor] ({x1:F2}cm,{y1:F2}cm) rectangle ({x1 + cellSize:F2}cm,{y1 + cellSize:F2}cm);");
                if (i != 8 - i) // Don't fill center twice
                {
                    sb.AppendLine($@"  \fill[diagcolor] ({x2:F2}cm,{y2:F2}cm) rectangle ({x2 + cellSize:F2}cm,{y2 + cellSize:F2}cm);");
                }
            }
        }
        else if (metadata?.Variant == Variant.ColorConstrained)
        {
            // Color each cell based on position within box
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    var posInBoxRow = row % 3;
                    var posInBoxCol = col % 3;
                    var colorIndex = posInBoxRow * 3 + posInBoxCol;
                    var color = GetTikZColorName(colorIndex);
                    double x = col * cellSize;
                    double y = (8 - row) * cellSize;
                    sb.AppendLine($@"  \fill[{color}] ({x:F2}cm,{y:F2}cm) rectangle ({x + cellSize:F2}cm,{y + cellSize:F2}cm);");
                }
            }
        }
        
        // Draw thin grid lines (all cell borders)
        sb.AppendLine($@"  \draw[line width={ThinLineWidth}pt, gray!60] (0,0) grid[step={cellSize:F2}cm] ({gridSize:F2}cm,{gridSize:F2}cm);");
        
        // Draw thick box lines (every 3 cells)
        double boxSize = cellSize * 3;
        sb.AppendLine($@"  \draw[line width={ThickLineWidth}pt, black] (0,0) grid[step={boxSize:F2}cm] ({gridSize:F2}cm,{gridSize:F2}cm);");
        
        // Draw outer border (thick)
        sb.AppendLine($@"  \draw[line width={ThickLineWidth}pt, black] (0,0) rectangle ({gridSize:F2}cm,{gridSize:F2}cm);");
        
        // Place numbers
        for (int row = 0; row < puzzle.Size; row++)
        {
            for (int col = 0; col < puzzle.Size; col++)
            {
                var value = puzzle[row, col];
                if (value != 0)
                {
                    // Calculate center of cell (TikZ y-axis is inverted relative to row)
                    double x = (col + 0.5) * cellSize;
                    double y = (8 - row + 0.5) * cellSize;
                    // Use \LARGE for font size (~2x larger); skip \bfseries if using bundled bold font
                    var fontStyle = _options.UseBundledFont ? @"\LARGE" : @"\LARGE\bfseries";
                    sb.AppendLine($@"  \node[font={fontStyle}] at ({x:F2}cm,{y:F2}cm) {{{value}}};");
                }
            }
        }
        
        sb.AppendLine(@"\end{tikzpicture}");
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates a TikZ grid with puzzle footer for multi-puzzle layouts.
    /// </summary>
    private string GenerateTikZGridWithFooter(Board puzzle, GeneratedPuzzle? metadata)
    {
        var sb = new StringBuilder();
        
        // Generate the grid
        sb.Append(GenerateTikZGrid(puzzle, metadata, 1.0));
        
        // Add footer with negative space to pull it tight against the puzzle
        if (metadata != null)
        {
            sb.AppendLine(@"\par\vspace{-0.15cm}");
            sb.AppendLine(@"{\tiny");
            var footerParts = new List<string>();
            
            if (metadata.Seed.HasValue)
            {
                footerParts.Add($@"\#{metadata.PuzzleNumber} (Seed: {metadata.Seed.Value})");
            }
            else
            {
                footerParts.Add($@"\#{metadata.PuzzleNumber}");
            }
            
            footerParts.Add($@"{metadata.GeneratedAt:yyyy-MM-dd HH:mm}");
            footerParts.Add($@"{metadata.Difficulty}");
            
            sb.AppendLine(string.Join(" | ", footerParts));
            sb.AppendLine(@"}");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates an empty TikZ grid for solving sheets.
    /// </summary>
    private string GenerateEmptyTikZGrid(int size, double scale)
    {
        var sb = new StringBuilder();
        double cellSize = GetCellSizeCm() * scale;
        double gridSize = cellSize * size;
        double boxSize = cellSize * 3;
        
        sb.AppendLine($@"\begin{{tikzpicture}}[scale=1]");
        
        // Draw thin grid lines
        sb.AppendLine($@"  \draw[line width={ThinLineWidth}pt, gray!60] (0,0) grid[step={cellSize:F2}cm] ({gridSize:F2}cm,{gridSize:F2}cm);");
        
        // Draw thick box lines
        sb.AppendLine($@"  \draw[line width={ThickLineWidth}pt, black] (0,0) grid[step={boxSize:F2}cm] ({gridSize:F2}cm,{gridSize:F2}cm);");
        
        // Draw outer border
        sb.AppendLine($@"  \draw[line width={ThickLineWidth}pt, black] (0,0) rectangle ({gridSize:F2}cm,{gridSize:F2}cm);");
        
        sb.AppendLine(@"\end{tikzpicture}");
        return sb.ToString();
    }
    
    /// <summary>
    /// Gets TikZ color name for color-constrained variant.
    /// </summary>
    private string GetTikZColorName(int index)
    {
        var colors = new[] { "sred", "spink", "sviolet", "sgrey", "sorange", "syellow", "slgreen", "sdgreen", "sblue" };
        return colors[index % colors.Length];
    }
    
    /// <summary>
    /// Balances puzzles across pages for even distribution.
    /// </summary>
    private List<int> BalancePuzzlesAcrossPages(int totalPuzzles, int maxPerPage)
    {
        var distribution = new List<int>();
        
        if (totalPuzzles <= maxPerPage)
        {
            distribution.Add(totalPuzzles);
            return distribution;
        }
        
        int pagesNeeded = (int)Math.Ceiling((double)totalPuzzles / maxPerPage);
        int basePerPage = totalPuzzles / pagesNeeded;
        int remainder = totalPuzzles % pagesNeeded;
        
        // Distribute evenly, with remainder spread across first pages
        for (int i = 0; i < pagesNeeded; i++)
        {
            int puzzlesThisPage = basePerPage;
            if (i < remainder)
            {
                puzzlesThisPage++;
            }
            distribution.Add(puzzlesThisPage);
        }
        
        return distribution;
    }
}
