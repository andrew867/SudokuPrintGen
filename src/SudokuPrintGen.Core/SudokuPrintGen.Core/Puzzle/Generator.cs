using SudokuPrintGen.Core.Solver;
using SudokuPrintGen.Core.Output;

namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Generates Sudoku puzzles with specified difficulty and variant.
/// </summary>
public class PuzzleGenerator
{
    private readonly ISolver _solver;
    private readonly Random _random;
    private readonly int? _seed;
    
    public PuzzleGenerator(ISolver? solver = null, int? seed = null)
    {
        _solver = solver ?? new DpllSolver();
        _seed = seed;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }
    
    /// <summary>
    /// Gets the seed used for generation (null if random).
    /// </summary>
    public int? Seed => _seed;
    
    private const int MaxGenerationAttempts = 100;
    private const int MaxSolutionAttempts = 50;
    
    /// <summary>
    /// Gets statistics about the last generation.
    /// </summary>
    public OutputStatistics? LastStatistics { get; private set; }
    
    /// <summary>
    /// Generates a puzzle with the specified parameters.
    /// </summary>
    public GeneratedPuzzle Generate(Difficulty difficulty, Variant variant = Variant.Classic, int size = 9, int boxRows = 3, int boxCols = 3)
    {
        var startTime = DateTime.UtcNow;
        int attempts = 0;
        
        for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            attempts++;
            
            // Start with a complete solved board
            var solution = GenerateCompleteSolution(size, boxRows, boxCols);
            if (solution == null)
            {
                continue; // Try again
            }
            
            // Create puzzle by removing clues
            var puzzle = CreatePuzzleFromSolution(solution, difficulty, size);
            
            // Validate puzzle
            var validation = BoardValidator.Validate(puzzle);
            if (!validation.IsValid)
            {
                continue; // Try again if invalid
            }
            
            // Verify uniqueness
            var solveStart = DateTime.UtcNow;
            if (_solver.HasUniqueSolution(puzzle))
            {
                var generationTime = DateTime.UtcNow - startTime;
                var solveTime = DateTime.UtcNow - solveStart;
                
                LastStatistics = new OutputStatistics
                {
                    GenerationTime = generationTime,
                    SolveTime = solveTime,
                    ClueCount = puzzle.GetClueCount(),
                    Attempts = attempts
                };
                
                // Analyze puzzle
                var rating = DifficultyRater.RatePuzzle(puzzle, _solver);
                var symmetry = SymmetryDetector.DetectSymmetry(puzzle);
                
                return new GeneratedPuzzle
                {
                    Puzzle = puzzle,
                    Solution = solution,
                    Difficulty = difficulty,
                    Variant = variant,
                    Seed = _seed,
                    GeneratedAt = DateTime.UtcNow,
                    SolverAlgorithm = "DPLL",
                    DifficultyRating = rating,
                    Symmetry = symmetry
                };
            }
        }
        
        // If we couldn't generate a unique puzzle after max attempts, throw exception
        throw new InvalidOperationException($"Failed to generate a unique puzzle after {MaxGenerationAttempts} attempts. Try a different seed or difficulty.");
    }
    
    private Board? GenerateCompleteSolution(int size, int boxRows, int boxCols)
    {
        for (int attempt = 0; attempt < MaxSolutionAttempts; attempt++)
        {
            var board = new Board(size, boxRows, boxCols);
            
            // Simple approach: fill diagonally first, then solve
            // For a more robust solution, we could use a backtracking approach
            FillDiagonalBoxes(board);
            
            // Solve the partially filled board
            var solver = new DpllSolver();
            var solution = solver.Solve(board);
            
            if (solution != null)
            {
                return solution;
            }
        }
        
        return null; // Failed to generate solution
    }
    
    private void FillDiagonalBoxes(Board board)
    {
        // Fill the diagonal boxes (top-left, middle, bottom-right for 9x9)
        var boxesPerRow = board.Size / board.BoxCols;
        
        for (int boxRow = 0; boxRow < boxesPerRow; boxRow++)
        {
            if (boxRow < boxesPerRow)
            {
                var boxIndex = boxRow * boxesPerRow + boxRow;
                FillBoxWithRandomValues(board, boxIndex);
            }
        }
    }
    
    private void FillBoxWithRandomValues(Board board, int boxIndex)
    {
        var values = Enumerable.Range(1, board.Size).ToList();
        Shuffle(values);
        
        var cellIndex = 0;
        foreach (var (row, col, _) in board.GetBoxCells(boxIndex))
        {
            board[row, col] = values[cellIndex];
            cellIndex++;
        }
    }
    
    private Board CreatePuzzleFromSolution(Board solution, Difficulty difficulty, int size)
    {
        var puzzle = solution.Clone();
        var targetClues = difficulty.GetTargetClues(size);
        var currentClues = size * size;
        var cellsToRemove = currentClues - targetClues;
        
        // Create list of all cell positions
        var positions = new List<(int row, int col)>();
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                positions.Add((row, col));
            }
        }
        
        Shuffle(positions);
        
        // Try to remove cells while maintaining uniqueness
        int removed = 0;
        foreach (var (row, col) in positions)
        {
            if (removed >= cellsToRemove)
                break;
            
            var value = puzzle[row, col];
            puzzle[row, col] = 0;
            
            // Check if still unique
            if (!_solver.HasUniqueSolution(puzzle))
            {
                // Restore the value if removing it breaks uniqueness
                puzzle[row, col] = value;
            }
            else
            {
                removed++;
            }
        }
        
        return puzzle;
    }
    
    private void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}

/// <summary>
/// Represents a generated puzzle with its solution.
/// </summary>
public class GeneratedPuzzle
{
    public Board Puzzle { get; set; } = null!;
    public Board Solution { get; set; } = null!;
    public Difficulty Difficulty { get; set; }
    public Variant Variant { get; set; }
    public int? Seed { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string SolverAlgorithm { get; set; } = "DPLL";
    public int PuzzleNumber { get; set; }
    public DifficultyRating? DifficultyRating { get; set; }
    public SymmetryInfo? Symmetry { get; set; }
}

