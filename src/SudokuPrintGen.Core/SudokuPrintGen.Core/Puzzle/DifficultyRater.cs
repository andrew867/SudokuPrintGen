using SudokuPrintGen.Core.Solver;

namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Rates the difficulty of a Sudoku puzzle based on solver metrics and solving techniques.
/// Uses iteration-based difficulty measurement as the primary metric.
/// </summary>
public static class DifficultyRater
{
    /// <summary>
    /// Rates a puzzle using the old clue-based method (backward compatibility).
    /// </summary>
    public static DifficultyRating RatePuzzle(Board puzzle, ISolver solver)
    {
        // Use the new metrics-based method
        return RatePuzzleWithMetrics(puzzle, solver);
    }
    
    /// <summary>
    /// Rates a puzzle using solver metrics for accurate difficulty measurement.
    /// This is the primary method for difficulty rating.
    /// </summary>
    public static DifficultyRating RatePuzzleWithMetrics(Board puzzle, ISolver solver)
    {
        var rating = new DifficultyRating();
        
        // Count clues
        rating.ClueCount = puzzle.GetClueCount();
        rating.EmptyCells = puzzle.Size * puzzle.Size - rating.ClueCount;
        
        // Solve and collect metrics
        var solverResult = solver.SolveWithMetrics(puzzle);
        
        // Transfer solver metrics to rating
        rating.IterationCount = solverResult.IterationCount;
        rating.MaxBacktrackDepth = solverResult.MaxBacktrackDepth;
        rating.GuessCount = solverResult.GuessCount;
        rating.PropagationCycles = solverResult.PropagationCycles;
        
        // Calculate composite score
        rating.CalculateCompositeScore();
        
        // Analyze solving techniques
        var workingBoard = puzzle.Clone();
        rating.RequiredTechniques = AnalyzeSolvingTechniques(workingBoard, solver, rating);
        
        // Estimate difficulty from metrics
        rating.EstimatedDifficulty = EstimateDifficultyFromMetrics(rating);
        
        // Calculate difficulty range
        rating.DifficultyRange = GetDifficultyRange(rating);
        
        return rating;
    }
    
    /// <summary>
    /// Quickly estimates difficulty without full analysis.
    /// Useful for rapid filtering during generation.
    /// </summary>
    public static Difficulty QuickEstimateDifficulty(Board puzzle, ISolver solver)
    {
        var result = solver.SolveWithMetrics(puzzle);
        return DifficultyTargets.GetDifficultyFromScore(result.DifficultyScore);
    }
    
    /// <summary>
    /// Checks if a puzzle matches a target difficulty level.
    /// </summary>
    public static bool MatchesDifficulty(Board puzzle, ISolver solver, Difficulty targetDifficulty)
    {
        var result = solver.SolveWithMetrics(puzzle);
        return DifficultyTargets.IsScoreInRange(result.DifficultyScore, targetDifficulty);
    }
    
    /// <summary>
    /// Compares a puzzle's difficulty to a target.
    /// </summary>
    public static DifficultyComparison CompareDifficulty(Board puzzle, ISolver solver, Difficulty targetDifficulty)
    {
        var result = solver.SolveWithMetrics(puzzle);
        return DifficultyTargets.CompareScoreToDifficulty(result.DifficultyScore, targetDifficulty);
    }
    
    private static List<string> AnalyzeSolvingTechniques(Board board, ISolver solver, DifficultyRating rating)
    {
        var techniques = new List<string>();
        var workingBoard = board.Clone();
        
        // Detect naked singles
        if (DetectNakedSingles(workingBoard))
        {
            techniques.Add("NakedSingles");
        }
        
        // Detect hidden singles
        if (DetectHiddenSingles(workingBoard))
        {
            techniques.Add("HiddenSingles");
        }
        
        // If we had guesses, advanced techniques are required
        if (rating.GuessCount > 0)
        {
            techniques.Add("Guessing");
            
            // Classify based on guess complexity
            if (rating.GuessCount > 5)
            {
                techniques.Add("AdvancedBacktracking");
            }
        }
        
        // Deep backtracking indicates complex logic
        if (rating.MaxBacktrackDepth > 10)
        {
            techniques.Add("DeepBacktracking");
        }
        
        // If basic techniques aren't enough, puzzle requires advanced techniques
        if (techniques.Count == 0 && rating.IterationCount > 15)
        {
            techniques.Add("Advanced");
        }
        
        return techniques;
    }
    
    /// <summary>
    /// Detects if naked singles exist in the puzzle.
    /// A naked single is a cell with only one possible candidate.
    /// </summary>
    public static bool DetectNakedSingles(Board board)
    {
        var size = board.Size;
        Span<uint> rowCandidates = stackalloc uint[size];
        Span<uint> colCandidates = stackalloc uint[size];
        Span<uint> boxCandidates = stackalloc uint[size];
        
        // Initialize candidate sets
        const uint allCandidates = 0x1FF;
        for (int i = 0; i < size; i++)
        {
            rowCandidates[i] = allCandidates;
            colCandidates[i] = allCandidates;
            boxCandidates[i] = allCandidates;
        }
        
        // Process given clues
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var value = board[row, col];
                if (value != 0)
                {
                    var mask = ~(1u << (value - 1));
                    rowCandidates[row] &= mask;
                    colCandidates[col] &= mask;
                    var boxIndex = board.GetBoxIndex(row, col);
                    boxCandidates[boxIndex] &= mask;
                }
            }
        }
        
        // Check for cells with exactly one candidate
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (board[row, col] == 0)
                {
                    var boxIndex = board.GetBoxIndex(row, col);
                    var candidates = rowCandidates[row] & colCandidates[col] & boxCandidates[boxIndex];
                    
                    // Count bits - if exactly 1 candidate, it's a naked single
                    if (System.Numerics.BitOperations.PopCount(candidates) == 1)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Detects if hidden singles exist in the puzzle.
    /// A hidden single is when a digit can only go in one cell within a row/col/box.
    /// </summary>
    public static bool DetectHiddenSingles(Board board)
    {
        var size = board.Size;
        
        // Check rows
        for (int row = 0; row < size; row++)
        {
            if (HasHiddenSingleInRow(board, row))
                return true;
        }
        
        // Check columns
        for (int col = 0; col < size; col++)
        {
            if (HasHiddenSingleInColumn(board, col))
                return true;
        }
        
        // Check boxes
        var boxCount = size;
        for (int box = 0; box < boxCount; box++)
        {
            if (HasHiddenSingleInBox(board, box))
                return true;
        }
        
        return false;
    }
    
    private static bool HasHiddenSingleInRow(Board board, int row)
    {
        var size = board.Size;
        
        // For each digit
        for (int digit = 1; digit <= size; digit++)
        {
            // Check if digit is already placed in row
            bool placed = false;
            for (int col = 0; col < size; col++)
            {
                if (board[row, col] == digit)
                {
                    placed = true;
                    break;
                }
            }
            
            if (placed) continue;
            
            // Count cells where digit can be placed
            int possibleCount = 0;
            for (int col = 0; col < size; col++)
            {
                if (board[row, col] == 0 && CanPlaceDigit(board, row, col, digit))
                {
                    possibleCount++;
                    if (possibleCount > 1) break;
                }
            }
            
            if (possibleCount == 1)
                return true;
        }
        
        return false;
    }
    
    private static bool HasHiddenSingleInColumn(Board board, int col)
    {
        var size = board.Size;
        
        // For each digit
        for (int digit = 1; digit <= size; digit++)
        {
            // Check if digit is already placed in column
            bool placed = false;
            for (int row = 0; row < size; row++)
            {
                if (board[row, col] == digit)
                {
                    placed = true;
                    break;
                }
            }
            
            if (placed) continue;
            
            // Count cells where digit can be placed
            int possibleCount = 0;
            for (int row = 0; row < size; row++)
            {
                if (board[row, col] == 0 && CanPlaceDigit(board, row, col, digit))
                {
                    possibleCount++;
                    if (possibleCount > 1) break;
                }
            }
            
            if (possibleCount == 1)
                return true;
        }
        
        return false;
    }
    
    private static bool HasHiddenSingleInBox(Board board, int boxIndex)
    {
        var size = board.Size;
        var boxCells = board.GetBoxCells(boxIndex).ToList();
        
        // For each digit
        for (int digit = 1; digit <= size; digit++)
        {
            // Check if digit is already placed in box
            bool placed = boxCells.Any(c => c.value == digit);
            if (placed) continue;
            
            // Count cells where digit can be placed
            int possibleCount = 0;
            foreach (var (row, col, value) in boxCells)
            {
                if (value == 0 && CanPlaceDigit(board, row, col, digit))
                {
                    possibleCount++;
                    if (possibleCount > 1) break;
                }
            }
            
            if (possibleCount == 1)
                return true;
        }
        
        return false;
    }
    
    private static bool CanPlaceDigit(Board board, int row, int col, int digit)
    {
        // Check row
        for (int c = 0; c < board.Size; c++)
        {
            if (board[row, c] == digit)
                return false;
        }
        
        // Check column
        for (int r = 0; r < board.Size; r++)
        {
            if (board[r, col] == digit)
                return false;
        }
        
        // Check box
        var boxIndex = board.GetBoxIndex(row, col);
        foreach (var (r, c, v) in board.GetBoxCells(boxIndex))
        {
            if (v == digit)
                return false;
        }
        
        return true;
    }
    
    private static Difficulty EstimateDifficultyFromMetrics(DifficultyRating rating)
    {
        // Primary: use composite score
        return DifficultyTargets.GetDifficultyFromScore(rating.CompositeScore);
    }
    
    private static (Difficulty min, Difficulty max) GetDifficultyRange(DifficultyRating rating)
    {
        // Calculate range based on metrics variance
        var baseDifficulty = rating.EstimatedDifficulty;
        var difficulties = Enum.GetValues<Difficulty>();
        var baseIndex = Array.IndexOf(difficulties, baseDifficulty);
        
        // Determine range based on score proximity to boundaries
        var (minScore, maxScore) = DifficultyTargets.GetScoreRange(baseDifficulty);
        var scorePosition = (rating.CompositeScore - minScore) / (maxScore - minScore);
        
        Difficulty minDiff, maxDiff;
        
        if (scorePosition < 0.2 && baseIndex > 0)
        {
            // Close to lower boundary
            minDiff = difficulties[baseIndex - 1];
            maxDiff = baseDifficulty;
        }
        else if (scorePosition > 0.8 && baseIndex < difficulties.Length - 1)
        {
            // Close to upper boundary
            minDiff = baseDifficulty;
            maxDiff = difficulties[baseIndex + 1];
        }
        else
        {
            // Solidly in range
            minDiff = baseDifficulty;
            maxDiff = baseDifficulty;
        }
        
        return (minDiff, maxDiff);
    }
}

/// <summary>
/// Rating information for a puzzle including solver metrics.
/// </summary>
public class DifficultyRating
{
    /// <summary>
    /// Number of clues (given digits) in the puzzle.
    /// </summary>
    public int ClueCount { get; set; }
    
    /// <summary>
    /// Number of empty cells to be filled.
    /// </summary>
    public int EmptyCells { get; set; }
    
    /// <summary>
    /// List of solving techniques required/detected.
    /// </summary>
    public List<string> RequiredTechniques { get; set; } = new();
    
    /// <summary>
    /// Estimated difficulty level.
    /// </summary>
    public Difficulty EstimatedDifficulty { get; set; }
    
    /// <summary>
    /// Legacy score property (use CompositeScore instead).
    /// </summary>
    public double Score 
    { 
        get => CompositeScore;
        set => CompositeScore = value;
    }
    
    /// <summary>
    /// Number of solver iterations required to solve.
    /// This is the primary difficulty metric.
    /// </summary>
    public int IterationCount { get; set; }
    
    /// <summary>
    /// Maximum backtracking depth reached during solving.
    /// </summary>
    public int MaxBacktrackDepth { get; set; }
    
    /// <summary>
    /// Number of guesses (branching decisions) made during solving.
    /// </summary>
    public int GuessCount { get; set; }
    
    /// <summary>
    /// Number of constraint propagation cycles during solving.
    /// </summary>
    public int PropagationCycles { get; set; }
    
    /// <summary>
    /// Composite difficulty score calculated from all metrics.
    /// Higher values indicate harder puzzles.
    /// </summary>
    public double CompositeScore { get; set; }
    
    /// <summary>
    /// The difficulty range this puzzle falls into (min, max).
    /// Accounts for edge cases near difficulty boundaries.
    /// </summary>
    public (Difficulty min, Difficulty max) DifficultyRange { get; set; }
    
    /// <summary>
    /// Whether the puzzle's difficulty matches a specific target.
    /// </summary>
    public bool IsInTargetRange { get; set; }
    
    /// <summary>
    /// The target difficulty this puzzle was generated for.
    /// </summary>
    public Difficulty? TargetDifficulty { get; set; }
    
    /// <summary>
    /// Calculates the composite difficulty score from metrics.
    /// </summary>
    public void CalculateCompositeScore()
    {
        // Weighted formula:
        // - Iteration count: 50% (primary metric)
        // - Max backtrack depth: 20%
        // - Guess count: 20%
        // - Clue ratio: 10% (fewer clues = harder)
        
        double iterationComponent = IterationCount * 0.50;
        double depthComponent = MaxBacktrackDepth * 2.0 * 0.20;
        double guessComponent = GuessCount * 3.0 * 0.20;
        
        // Clue ratio component (inverted - fewer clues = higher score)
        double totalCells = ClueCount + EmptyCells;
        double clueRatio = totalCells > 0 ? ClueCount / totalCells : 0.5;
        double clueComponent = (1.0 - clueRatio) * 20.0 * 0.10;
        
        CompositeScore = iterationComponent + depthComponent + guessComponent + clueComponent;
    }
    
    /// <summary>
    /// Checks if the rating matches a target difficulty.
    /// </summary>
    public bool MatchesDifficulty(Difficulty target)
    {
        return DifficultyTargets.IsScoreInRange(CompositeScore, target);
    }
    
    /// <summary>
    /// Compares this rating to a target difficulty.
    /// </summary>
    public DifficultyComparison CompareTo(Difficulty target)
    {
        return DifficultyTargets.CompareScoreToDifficulty(CompositeScore, target);
    }
}
