namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Detects various types of symmetry in Sudoku puzzles.
/// </summary>
public static class SymmetryDetector
{
    /// <summary>
    /// Detects all types of symmetry in a puzzle.
    /// </summary>
    public static SymmetryInfo DetectSymmetry(Board puzzle)
    {
        var info = new SymmetryInfo();
        
        info.HasRotationalSymmetry = HasRotationalSymmetry(puzzle);
        info.HasHorizontalReflection = HasHorizontalReflection(puzzle);
        info.HasVerticalReflection = HasVerticalReflection(puzzle);
        info.HasDiagonalSymmetry = HasDiagonalSymmetry(puzzle);
        info.SymmetryScore = CalculateSymmetryScore(info);
        
        return info;
    }
    
    private static bool HasRotationalSymmetry(Board puzzle)
    {
        // Check 180-degree rotational symmetry
        var size = puzzle.Size;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var oppositeRow = size - 1 - row;
                var oppositeCol = size - 1 - col;
                
                // If one cell is filled, the opposite must also be filled (or both empty)
                var value1 = puzzle[row, col];
                var value2 = puzzle[oppositeRow, oppositeCol];
                
                if ((value1 == 0) != (value2 == 0))
                {
                    return false;
                }
            }
        }
        return true;
    }
    
    private static bool HasHorizontalReflection(Board puzzle)
    {
        var size = puzzle.Size;
        for (int row = 0; row < size / 2; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var reflectedRow = size - 1 - row;
                var value1 = puzzle[row, col];
                var value2 = puzzle[reflectedRow, col];
                
                if ((value1 == 0) != (value2 == 0))
                {
                    return false;
                }
            }
        }
        return true;
    }
    
    private static bool HasVerticalReflection(Board puzzle)
    {
        var size = puzzle.Size;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size / 2; col++)
            {
                var reflectedCol = size - 1 - col;
                var value1 = puzzle[row, col];
                var value2 = puzzle[row, reflectedCol];
                
                if ((value1 == 0) != (value2 == 0))
                {
                    return false;
                }
            }
        }
        return true;
    }
    
    private static bool HasDiagonalSymmetry(Board puzzle)
    {
        // Check main diagonal symmetry
        var size = puzzle.Size;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (row != col)
                {
                    var value1 = puzzle[row, col];
                    var value2 = puzzle[col, row];
                    
                    if ((value1 == 0) != (value2 == 0))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    
    private static double CalculateSymmetryScore(SymmetryInfo info)
    {
        double score = 0.0;
        if (info.HasRotationalSymmetry) score += 0.3;
        if (info.HasHorizontalReflection) score += 0.25;
        if (info.HasVerticalReflection) score += 0.25;
        if (info.HasDiagonalSymmetry) score += 0.2;
        return score;
    }
}

/// <summary>
/// Information about puzzle symmetry.
/// </summary>
public class SymmetryInfo
{
    public bool HasRotationalSymmetry { get; set; }
    public bool HasHorizontalReflection { get; set; }
    public bool HasVerticalReflection { get; set; }
    public bool HasDiagonalSymmetry { get; set; }
    public double SymmetryScore { get; set; }
    
    public List<string> GetSymmetryTypes()
    {
        var types = new List<string>();
        if (HasRotationalSymmetry) types.Add("Rotational");
        if (HasHorizontalReflection) types.Add("HorizontalReflection");
        if (HasVerticalReflection) types.Add("VerticalReflection");
        if (HasDiagonalSymmetry) types.Add("Diagonal");
        return types;
    }
}

