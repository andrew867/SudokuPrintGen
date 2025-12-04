using System.Text;

namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Validates Sudoku boards for correctness.
/// </summary>
public static class BoardValidator
{
    /// <summary>
    /// Validates that a board follows Sudoku rules.
    /// </summary>
    public static ValidationResult Validate(Board board)
    {
        var errors = new List<string>();
        
        // Check rows
        for (int row = 0; row < board.Size; row++)
        {
            var seen = new HashSet<int>();
            for (int col = 0; col < board.Size; col++)
            {
                var value = board[row, col];
                if (value != 0)
                {
                    if (value < 1 || value > board.Size)
                    {
                        errors.Add($"Invalid value {value} at row {row}, col {col}");
                    }
                    else if (seen.Contains(value))
                    {
                        errors.Add($"Duplicate value {value} in row {row}");
                    }
                    else
                    {
                        seen.Add(value);
                    }
                }
            }
        }
        
        // Check columns
        for (int col = 0; col < board.Size; col++)
        {
            var seen = new HashSet<int>();
            for (int row = 0; row < board.Size; row++)
            {
                var value = board[row, col];
                if (value != 0 && seen.Contains(value))
                {
                    errors.Add($"Duplicate value {value} in column {col}");
                }
                else if (value != 0)
                {
                    seen.Add(value);
                }
            }
        }
        
        // Check boxes
        var boxesPerRow = board.Size / board.BoxCols;
        var boxesPerCol = board.Size / board.BoxRows;
        for (int boxRow = 0; boxRow < boxesPerCol; boxRow++)
        {
            for (int boxCol = 0; boxCol < boxesPerRow; boxCol++)
            {
                var boxIndex = boxRow * boxesPerRow + boxCol;
                var seen = new HashSet<int>();
                foreach (var (r, c, v) in board.GetBoxCells(boxIndex))
                {
                    if (v != 0 && seen.Contains(v))
                    {
                        errors.Add($"Duplicate value {v} in box {boxIndex} (row {r}, col {c})");
                    }
                    else if (v != 0)
                    {
                        seen.Add(v);
                    }
                }
            }
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

/// <summary>
/// Result of board validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public string GetErrorMessage()
    {
        if (IsValid)
            return "Board is valid";
        
        var sb = new StringBuilder();
        sb.AppendLine($"Board validation failed with {Errors.Count} error(s):");
        foreach (var error in Errors)
        {
            sb.AppendLine($"  - {error}");
        }
        return sb.ToString();
    }
}

