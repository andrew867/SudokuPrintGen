using System.Text;

namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Tracks statistics across batch puzzle generation.
/// </summary>
public class GenerationStatistics
{
    private readonly List<PuzzleStats> _puzzleStats = new();
    private readonly object _lock = new();
    
    /// <summary>
    /// Total number of puzzles generated.
    /// </summary>
    public int TotalPuzzles => _puzzleStats.Count;
    
    /// <summary>
    /// Count of puzzles by difficulty level.
    /// </summary>
    public Dictionary<Difficulty, int> PuzzlesByDifficulty
    {
        get
        {
            lock (_lock)
            {
                return _puzzleStats
                    .GroupBy(p => p.TargetDifficulty)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }
    }
    
    /// <summary>
    /// Average iteration count per difficulty level.
    /// </summary>
    public Dictionary<Difficulty, double> AverageIterationCount
    {
        get
        {
            lock (_lock)
            {
                return _puzzleStats
                    .GroupBy(p => p.TargetDifficulty)
                    .ToDictionary(g => g.Key, g => g.Average(p => p.IterationCount));
            }
        }
    }
    
    /// <summary>
    /// Standard deviation of iteration counts per difficulty level.
    /// </summary>
    public Dictionary<Difficulty, double> DifficultyVariance
    {
        get
        {
            lock (_lock)
            {
                return _puzzleStats
                    .GroupBy(p => p.TargetDifficulty)
                    .ToDictionary(g => g.Key, g => CalculateStdDev(g.Select(p => (double)p.IterationCount)));
            }
        }
    }
    
    /// <summary>
    /// Success rate (% puzzles matching target difficulty) per difficulty level.
    /// </summary>
    public Dictionary<Difficulty, double> SuccessRate
    {
        get
        {
            lock (_lock)
            {
                return _puzzleStats
                    .GroupBy(p => p.TargetDifficulty)
                    .ToDictionary(g => g.Key, g => 
                        g.Count() > 0 ? (double)g.Count(p => p.MatchedTarget) / g.Count() * 100 : 0);
            }
        }
    }
    
    /// <summary>
    /// Average refinement iterations per difficulty level.
    /// </summary>
    public Dictionary<Difficulty, double> AverageRefinementIterations
    {
        get
        {
            lock (_lock)
            {
                return _puzzleStats
                    .GroupBy(p => p.TargetDifficulty)
                    .ToDictionary(g => g.Key, g => g.Average(p => p.RefinementIterations));
            }
        }
    }
    
    /// <summary>
    /// Average composite score per difficulty level.
    /// </summary>
    public Dictionary<Difficulty, double> AverageCompositeScore
    {
        get
        {
            lock (_lock)
            {
                return _puzzleStats
                    .GroupBy(p => p.TargetDifficulty)
                    .ToDictionary(g => g.Key, g => g.Average(p => p.CompositeScore));
            }
        }
    }
    
    /// <summary>
    /// Adds a puzzle to the statistics.
    /// </summary>
    public void AddPuzzle(GeneratedPuzzle puzzle)
    {
        var stats = new PuzzleStats
        {
            TargetDifficulty = puzzle.Difficulty,
            ActualDifficulty = puzzle.DifficultyRating?.EstimatedDifficulty ?? puzzle.Difficulty,
            IterationCount = puzzle.DifficultyRating?.IterationCount ?? 0,
            CompositeScore = puzzle.DifficultyRating?.CompositeScore ?? 0,
            ClueCount = puzzle.Puzzle.GetClueCount(),
            MatchedTarget = puzzle.DifficultyRating?.IsInTargetRange ?? false,
            RefinementIterations = 0, // Would need to track this separately
            GuessCount = puzzle.DifficultyRating?.GuessCount ?? 0,
            MaxBacktrackDepth = puzzle.DifficultyRating?.MaxBacktrackDepth ?? 0
        };
        
        lock (_lock)
        {
            _puzzleStats.Add(stats);
        }
    }
    
    /// <summary>
    /// Adds puzzle statistics directly.
    /// </summary>
    public void AddStats(PuzzleStats stats)
    {
        lock (_lock)
        {
            _puzzleStats.Add(stats);
        }
    }
    
    /// <summary>
    /// Resets all statistics.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _puzzleStats.Clear();
        }
    }
    
    /// <summary>
    /// Gets a formatted report of the statistics.
    /// </summary>
    public string GetReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Generation Statistics Report ===");
        sb.AppendLine();
        sb.AppendLine($"Total Puzzles Generated: {TotalPuzzles}");
        sb.AppendLine();
        
        if (TotalPuzzles == 0)
        {
            sb.AppendLine("No puzzles generated yet.");
            return sb.ToString();
        }
        
        // Table header
        sb.AppendLine("Difficulty    | Count | Avg Iter | Std Dev | Success % | Avg Score");
        sb.AppendLine("--------------|-------|----------|---------|-----------|----------");
        
        foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
        {
            var count = PuzzlesByDifficulty.GetValueOrDefault(difficulty, 0);
            if (count == 0) continue;
            
            var avgIter = AverageIterationCount.GetValueOrDefault(difficulty, 0);
            var stdDev = DifficultyVariance.GetValueOrDefault(difficulty, 0);
            var success = SuccessRate.GetValueOrDefault(difficulty, 0);
            var avgScore = AverageCompositeScore.GetValueOrDefault(difficulty, 0);
            
            sb.AppendLine($"{difficulty,-13} | {count,5} | {avgIter,8:F1} | {stdDev,7:F2} | {success,8:F1}% | {avgScore,8:F1}");
        }
        
        sb.AppendLine();
        sb.AppendLine("=== Difficulty Targets Reference ===");
        sb.AppendLine();
        sb.AppendLine("Difficulty | Iteration Range | Score Range");
        sb.AppendLine("-----------|-----------------|-------------");
        
        foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
        {
            var (iterMin, iterMax) = DifficultyTargets.GetIterationRange(difficulty);
            var (scoreMin, scoreMax) = DifficultyTargets.GetScoreRange(difficulty);
            
            string iterRange = iterMax == int.MaxValue ? $"{iterMin}+" : $"{iterMin}-{iterMax}";
            string scoreRange = scoreMax == double.MaxValue ? $"{scoreMin:F0}+" : $"{scoreMin:F0}-{scoreMax:F0}";
            
            sb.AppendLine($"{difficulty,-10} | {iterRange,-15} | {scoreRange}");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Gets detailed statistics for a specific difficulty level.
    /// </summary>
    public DifficultyStatistics GetDetailedStats(Difficulty difficulty)
    {
        lock (_lock)
        {
            var puzzlesForDifficulty = _puzzleStats.Where(p => p.TargetDifficulty == difficulty).ToList();
            
            if (puzzlesForDifficulty.Count == 0)
            {
                return new DifficultyStatistics { Difficulty = difficulty };
            }
            
            return new DifficultyStatistics
            {
                Difficulty = difficulty,
                Count = puzzlesForDifficulty.Count,
                AverageIterationCount = puzzlesForDifficulty.Average(p => p.IterationCount),
                MinIterationCount = puzzlesForDifficulty.Min(p => p.IterationCount),
                MaxIterationCount = puzzlesForDifficulty.Max(p => p.IterationCount),
                IterationStdDev = CalculateStdDev(puzzlesForDifficulty.Select(p => (double)p.IterationCount)),
                AverageCompositeScore = puzzlesForDifficulty.Average(p => p.CompositeScore),
                SuccessCount = puzzlesForDifficulty.Count(p => p.MatchedTarget),
                SuccessRate = (double)puzzlesForDifficulty.Count(p => p.MatchedTarget) / puzzlesForDifficulty.Count * 100,
                AverageClueCount = puzzlesForDifficulty.Average(p => p.ClueCount),
                AverageGuessCount = puzzlesForDifficulty.Average(p => p.GuessCount),
                AverageBacktrackDepth = puzzlesForDifficulty.Average(p => p.MaxBacktrackDepth)
            };
        }
    }
    
    private static double CalculateStdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count <= 1) return 0;
        
        var avg = list.Average();
        var sumOfSquares = list.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sumOfSquares / (list.Count - 1));
    }
}

/// <summary>
/// Statistics for a single generated puzzle.
/// </summary>
public class PuzzleStats
{
    public Difficulty TargetDifficulty { get; set; }
    public Difficulty ActualDifficulty { get; set; }
    public int IterationCount { get; set; }
    public double CompositeScore { get; set; }
    public int ClueCount { get; set; }
    public bool MatchedTarget { get; set; }
    public int RefinementIterations { get; set; }
    public int GuessCount { get; set; }
    public int MaxBacktrackDepth { get; set; }
}

/// <summary>
/// Detailed statistics for a specific difficulty level.
/// </summary>
public class DifficultyStatistics
{
    public Difficulty Difficulty { get; set; }
    public int Count { get; set; }
    public double AverageIterationCount { get; set; }
    public int MinIterationCount { get; set; }
    public int MaxIterationCount { get; set; }
    public double IterationStdDev { get; set; }
    public double AverageCompositeScore { get; set; }
    public int SuccessCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageClueCount { get; set; }
    public double AverageGuessCount { get; set; }
    public double AverageBacktrackDepth { get; set; }
}

