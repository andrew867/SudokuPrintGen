namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Distributes difficulties across multiple puzzles.
/// </summary>
public static class DifficultyDistributor
{
    /// <summary>
    /// Distributes difficulties across a number of puzzles.
    /// For alternating pattern: cycles through difficulties in groups.
    /// </summary>
    public static List<Difficulty> DistributeDifficulties(List<Difficulty> difficulties, int count)
    {
        if (difficulties == null || difficulties.Count == 0)
        {
            return Enumerable.Repeat(Difficulty.Medium, count).ToList();
        }
        
        if (difficulties.Count == 1)
        {
            return Enumerable.Repeat(difficulties[0], count).ToList();
        }
        
        var result = new List<Difficulty>();
        
        // Calculate base distribution
        int baseCount = count / difficulties.Count;
        int remainder = count % difficulties.Count;
        
        // For alternating pattern: use groups of 2 (or baseCount if > 2)
        int groupSize = Math.Max(2, baseCount);
        
        if (difficulties.Count == 2)
        {
            // Two difficulties: alternate in groups of 2
            // Example: 10 puzzles with easy,medium = 2 easy, 2 medium, 2 easy, 2 medium, 2 easy
            int index = 0;
            while (result.Count < count)
            {
                var difficulty = difficulties[index % difficulties.Count];
                int currentGroupSize = 2; // Always use groups of 2 for two difficulties
                
                // For remainder, add extra to first difficulty
                if (result.Count + currentGroupSize > count)
                {
                    currentGroupSize = count - result.Count;
                }
                
                result.AddRange(Enumerable.Repeat(difficulty, currentGroupSize));
                index++;
            }
        }
        else
        {
            // Three or more difficulties: cycle through in groups
            // Example: 3 difficulties, 10 puzzles = 2 in first, 2 in second, 2 in third, 2 in first, 2 in second
            int index = 0;
            while (result.Count < count)
            {
                var difficulty = difficulties[index % difficulties.Count];
                int currentGroupSize = groupSize;
                
                // For remainder, distribute to first few difficulties
                if (result.Count + currentGroupSize > count)
                {
                    currentGroupSize = count - result.Count;
                }
                
                result.AddRange(Enumerable.Repeat(difficulty, currentGroupSize));
                index++;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Parses a comma-separated difficulty string.
    /// </summary>
    public static List<Difficulty> ParseDifficulties(string difficultyStr)
    {
        if (string.IsNullOrWhiteSpace(difficultyStr))
        {
            return new List<Difficulty> { Difficulty.Medium };
        }
        
        var parts = difficultyStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var difficulties = new List<Difficulty>();
        
        foreach (var part in parts)
        {
            if (Enum.TryParse<Difficulty>(part, true, out var difficulty))
            {
                difficulties.Add(difficulty);
            }
        }
        
        return difficulties.Count > 0 ? difficulties : new List<Difficulty> { Difficulty.Medium };
    }
}

