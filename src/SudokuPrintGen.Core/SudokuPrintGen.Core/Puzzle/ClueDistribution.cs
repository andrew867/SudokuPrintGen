namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Contains information about the distribution of clues across a puzzle.
/// </summary>
public class ClueDistribution
{
    /// <summary>
    /// Number of clues in each row.
    /// </summary>
    public int[] CluesPerRow { get; set; } = Array.Empty<int>();
    
    /// <summary>
    /// Number of clues in each column.
    /// </summary>
    public int[] CluesPerColumn { get; set; } = Array.Empty<int>();
    
    /// <summary>
    /// Number of clues in each box.
    /// </summary>
    public int[] CluesPerBox { get; set; } = Array.Empty<int>();
    
    /// <summary>
    /// Total number of clues in the puzzle.
    /// </summary>
    public int TotalClues { get; set; }
    
    /// <summary>
    /// Average number of clues per region (row/column/box).
    /// </summary>
    public double AverageCluesPerRegion { get; set; }
    
    /// <summary>
    /// Variance of clue distribution across all regions.
    /// Lower variance indicates more uniform distribution.
    /// </summary>
    public double Variance { get; set; }
    
    /// <summary>
    /// Standard deviation of clue distribution.
    /// </summary>
    public double StandardDeviation => Math.Sqrt(Variance);
    
    /// <summary>
    /// Regions with significantly more clues than average.
    /// </summary>
    public List<(string type, int index, int clueCount)> OverConstrainedRegions { get; set; } = new();
    
    /// <summary>
    /// Regions with significantly fewer clues than average.
    /// </summary>
    public List<(string type, int index, int clueCount)> UnderConstrainedRegions { get; set; } = new();
    
    /// <summary>
    /// Creates a ClueDistribution for a given board.
    /// </summary>
    public static ClueDistribution FromBoard(Board board)
    {
        var distribution = new ClueDistribution
        {
            CluesPerRow = new int[board.Size],
            CluesPerColumn = new int[board.Size],
            CluesPerBox = new int[board.Size]
        };
        
        // Count clues in each region
        for (int row = 0; row < board.Size; row++)
        {
            for (int col = 0; col < board.Size; col++)
            {
                if (board[row, col] != 0)
                {
                    distribution.CluesPerRow[row]++;
                    distribution.CluesPerColumn[col]++;
                    distribution.CluesPerBox[board.GetBoxIndex(row, col)]++;
                    distribution.TotalClues++;
                }
            }
        }
        
        // Calculate average
        var allCounts = distribution.CluesPerRow
            .Concat(distribution.CluesPerColumn)
            .Concat(distribution.CluesPerBox)
            .ToArray();
        
        distribution.AverageCluesPerRegion = allCounts.Length > 0 
            ? allCounts.Average() 
            : 0;
        
        // Calculate variance
        if (allCounts.Length > 0)
        {
            var avg = distribution.AverageCluesPerRegion;
            distribution.Variance = allCounts.Average(x => Math.Pow(x - avg, 2));
        }
        
        // Identify over/under constrained regions
        var threshold = distribution.StandardDeviation;
        
        for (int i = 0; i < board.Size; i++)
        {
            // Rows
            if (distribution.CluesPerRow[i] > distribution.AverageCluesPerRegion + threshold)
            {
                distribution.OverConstrainedRegions.Add(("Row", i, distribution.CluesPerRow[i]));
            }
            else if (distribution.CluesPerRow[i] < distribution.AverageCluesPerRegion - threshold)
            {
                distribution.UnderConstrainedRegions.Add(("Row", i, distribution.CluesPerRow[i]));
            }
            
            // Columns
            if (distribution.CluesPerColumn[i] > distribution.AverageCluesPerRegion + threshold)
            {
                distribution.OverConstrainedRegions.Add(("Column", i, distribution.CluesPerColumn[i]));
            }
            else if (distribution.CluesPerColumn[i] < distribution.AverageCluesPerRegion - threshold)
            {
                distribution.UnderConstrainedRegions.Add(("Column", i, distribution.CluesPerColumn[i]));
            }
            
            // Boxes
            if (distribution.CluesPerBox[i] > distribution.AverageCluesPerRegion + threshold)
            {
                distribution.OverConstrainedRegions.Add(("Box", i, distribution.CluesPerBox[i]));
            }
            else if (distribution.CluesPerBox[i] < distribution.AverageCluesPerRegion - threshold)
            {
                distribution.UnderConstrainedRegions.Add(("Box", i, distribution.CluesPerBox[i]));
            }
        }
        
        return distribution;
    }
    
    /// <summary>
    /// Gets the clue count for a specific cell's row.
    /// </summary>
    public int GetRowClueCount(int row) => CluesPerRow[row];
    
    /// <summary>
    /// Gets the clue count for a specific cell's column.
    /// </summary>
    public int GetColumnClueCount(int col) => CluesPerColumn[col];
    
    /// <summary>
    /// Gets the clue count for a specific cell's box.
    /// </summary>
    public int GetBoxClueCount(int boxIndex) => CluesPerBox[boxIndex];
    
    /// <summary>
    /// Gets the total constraint level for a cell (sum of clues in row + col + box).
    /// </summary>
    public int GetCellConstraintLevel(Board board, int row, int col)
    {
        var boxIndex = board.GetBoxIndex(row, col);
        return CluesPerRow[row] + CluesPerColumn[col] + CluesPerBox[boxIndex];
    }
    
    /// <summary>
    /// Checks if a region is over-constrained.
    /// </summary>
    public bool IsOverConstrained(string type, int index)
    {
        return OverConstrainedRegions.Any(r => r.type == type && r.index == index);
    }
    
    /// <summary>
    /// Checks if a region is under-constrained.
    /// </summary>
    public bool IsUnderConstrained(string type, int index)
    {
        return UnderConstrainedRegions.Any(r => r.type == type && r.index == index);
    }
}

