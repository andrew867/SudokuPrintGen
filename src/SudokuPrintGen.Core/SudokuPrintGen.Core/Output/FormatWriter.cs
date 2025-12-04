using SudokuPrintGen.Core.Puzzle;
using SudokuPrintGen.Core.LaTeX;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SudokuPrintGen.Core.Output;

/// <summary>
/// Writes puzzles to various output formats.
/// </summary>
public class FormatWriter
{
    private readonly LaTeXGenerator _latexGenerator;
    
    public FormatWriter(LaTeXStyleOptions? styleOptions = null)
    {
        _latexGenerator = new LaTeXGenerator(styleOptions);
    }
    
    /// <summary>
    /// Writes puzzle to plain text format (81-char string).
    /// </summary>
    public string WriteText(Board puzzle)
    {
        return puzzle.ToString(useDots: true);
    }
    
    /// <summary>
    /// Writes puzzle to LaTeX format.
    /// </summary>
    public string WriteLaTeX(Board puzzle, Board? solution = null, GeneratedPuzzle? generatedPuzzle = null)
    {
        return _latexGenerator.Generate(puzzle, solution, generatedPuzzle);
    }
    
    /// <summary>
    /// Writes puzzle to JSON format.
    /// </summary>
    public string WriteJson(Board puzzle, Board? solution = null, GeneratedPuzzle? generatedPuzzle = null)
    {
        // Convert board to 2D array
        var puzzleArray = new int[puzzle.Size][];
        for (int row = 0; row < puzzle.Size; row++)
        {
            puzzleArray[row] = new int[puzzle.Size];
            for (int col = 0; col < puzzle.Size; col++)
            {
                puzzleArray[row][col] = puzzle[row, col];
            }
        }
        
        // Convert solution to 2D array if provided
        int[][]? solutionArray = null;
        if (solution != null)
        {
            solutionArray = new int[solution.Size][];
            for (int row = 0; row < solution.Size; row++)
            {
                solutionArray[row] = new int[solution.Size];
                for (int col = 0; col < solution.Size; col++)
                {
                    solutionArray[row][col] = solution[row, col];
                }
            }
        }
        
        // Create JSON object
        var jsonObject = new Dictionary<string, object?>
        {
            ["puzzle"] = puzzleArray
        };
        
        if (solutionArray != null)
        {
            jsonObject["solution"] = solutionArray;
        }
        
        if (generatedPuzzle != null)
        {
            jsonObject["difficulty"] = generatedPuzzle.Difficulty.ToString();
            jsonObject["variant"] = generatedPuzzle.Variant.ToString();
            
            if (generatedPuzzle.Seed.HasValue)
            {
                jsonObject["seed"] = generatedPuzzle.Seed.Value;
            }
            
            jsonObject["puzzleNumber"] = generatedPuzzle.PuzzleNumber;
            jsonObject["generatedAt"] = generatedPuzzle.GeneratedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
            jsonObject["solverAlgorithm"] = generatedPuzzle.SolverAlgorithm;
            jsonObject["clueCount"] = puzzle.GetClueCount();
            
            // Add difficulty rating if available
            if (generatedPuzzle.DifficultyRating != null)
            {
                jsonObject["difficultyRating"] = new Dictionary<string, object?>
                {
                    ["clueCount"] = generatedPuzzle.DifficultyRating.ClueCount,
                    ["emptyCells"] = generatedPuzzle.DifficultyRating.EmptyCells,
                    ["requiredTechniques"] = generatedPuzzle.DifficultyRating.RequiredTechniques,
                    ["estimatedDifficulty"] = generatedPuzzle.DifficultyRating.EstimatedDifficulty.ToString()
                };
            }
            
            // Add symmetry info if available
            if (generatedPuzzle.Symmetry != null)
            {
                jsonObject["symmetry"] = new Dictionary<string, object?>
                {
                    ["hasRotationalSymmetry"] = generatedPuzzle.Symmetry.HasRotationalSymmetry,
                    ["hasHorizontalReflection"] = generatedPuzzle.Symmetry.HasHorizontalReflection,
                    ["hasVerticalReflection"] = generatedPuzzle.Symmetry.HasVerticalReflection,
                    ["hasDiagonalSymmetry"] = generatedPuzzle.Symmetry.HasDiagonalSymmetry,
                    ["symmetryScore"] = generatedPuzzle.Symmetry.SymmetryScore,
                    ["symmetryTypes"] = generatedPuzzle.Symmetry.GetSymmetryTypes()
                };
            }
        }
        
        // Serialize with pretty printing
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return JsonSerializer.Serialize(jsonObject, options);
    }
}

