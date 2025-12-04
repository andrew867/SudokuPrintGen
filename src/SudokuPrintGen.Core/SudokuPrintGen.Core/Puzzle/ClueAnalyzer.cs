using SudokuPrintGen.Core.Solver;

namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Analyzes clue distribution and importance in Sudoku puzzles.
/// Used for strategic clue management during difficulty refinement.
/// </summary>
public static class ClueAnalyzer
{
    /// <summary>
    /// Analyzes the distribution of clues across a puzzle.
    /// </summary>
    public static ClueDistribution AnalyzeClueDistribution(Board puzzle)
    {
        return ClueDistribution.FromBoard(puzzle);
    }
    
    /// <summary>
    /// Calculates how important a specific clue is for maintaining the puzzle's difficulty.
    /// Returns a score from 0.0 (not important) to 1.0 (very important).
    /// </summary>
    public static double GetClueImportance(Board puzzle, int row, int col, Board solution, ISolver solver)
    {
        if (puzzle[row, col] == 0)
            return 0.0; // Not a clue
        
        // Test what happens if we remove this clue
        var testBoard = puzzle.Clone();
        testBoard[row, col] = 0;
        
        // Check uniqueness
        var solutionCount = solver.CountSolutions(testBoard, 2);
        
        if (solutionCount != 1)
        {
            // This clue is essential for uniqueness
            return 1.0;
        }
        
        // Get metrics with and without the clue
        var originalMetrics = solver.SolveWithMetrics(puzzle);
        var withoutClueMetrics = solver.SolveWithMetrics(testBoard);
        
        // Calculate importance based on difficulty impact
        var difficultyIncrease = withoutClueMetrics.DifficultyScore - originalMetrics.DifficultyScore;
        
        // Normalize to 0-1 range (capped)
        // A significant increase in difficulty means the clue was important
        var importance = Math.Min(1.0, Math.Max(0.0, difficultyIncrease / 50.0));
        
        // Factor in clue distribution
        var distribution = ClueDistribution.FromBoard(puzzle);
        var boxIndex = puzzle.GetBoxIndex(row, col);
        
        // Clues in under-constrained regions are more important
        if (distribution.IsUnderConstrained("Row", row) ||
            distribution.IsUnderConstrained("Column", col) ||
            distribution.IsUnderConstrained("Box", boxIndex))
        {
            importance = Math.Min(1.0, importance + 0.2);
        }
        
        return importance;
    }
    
    /// <summary>
    /// Finds all clue positions sorted by their importance (least important first).
    /// Useful for finding clues to remove when increasing difficulty.
    /// </summary>
    public static List<(int row, int col, double importance)> GetCluesByImportance(
        Board puzzle, Board solution, ISolver solver)
    {
        var clues = new List<(int row, int col, double importance)>();
        
        for (int row = 0; row < puzzle.Size; row++)
        {
            for (int col = 0; col < puzzle.Size; col++)
            {
                if (puzzle[row, col] != 0)
                {
                    var importance = GetClueImportance(puzzle, row, col, solution, solver);
                    clues.Add((row, col, importance));
                }
            }
        }
        
        // Sort by importance (least important first)
        return clues.OrderBy(c => c.importance).ToList();
    }
    
    /// <summary>
    /// Gets candidate positions for adding clues, sorted by effectiveness.
    /// Useful for simplifying puzzles when they're too hard.
    /// </summary>
    public static List<(int row, int col, int value, double effectiveness)> GetCandidateCluePositions(
        Board puzzle, Board solution, ISolver solver)
    {
        var candidates = new List<(int row, int col, int value, double effectiveness)>();
        var originalMetrics = solver.SolveWithMetrics(puzzle);
        
        for (int row = 0; row < puzzle.Size; row++)
        {
            for (int col = 0; col < puzzle.Size; col++)
            {
                if (puzzle[row, col] == 0)
                {
                    var value = solution[row, col];
                    var testBoard = puzzle.Clone();
                    testBoard[row, col] = value;
                    
                    var newMetrics = solver.SolveWithMetrics(testBoard);
                    
                    // Effectiveness is how much it reduces difficulty
                    var difficultyReduction = originalMetrics.DifficultyScore - newMetrics.DifficultyScore;
                    var effectiveness = Math.Max(0.0, difficultyReduction / 50.0);
                    
                    candidates.Add((row, col, value, effectiveness));
                }
            }
        }
        
        // Sort by effectiveness (most effective first)
        return candidates.OrderByDescending(c => c.effectiveness).ToList();
    }
    
    /// <summary>
    /// Finds symmetrical clue positions in the puzzle (180-degree rotational symmetry).
    /// </summary>
    public static List<((int row, int col) pos1, (int row, int col) pos2)> FindSymmetricalPairs(Board puzzle)
    {
        var pairs = new List<((int, int), (int, int))>();
        var size = puzzle.Size;
        
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                // Check 180-degree rotational symmetry
                int symRow = size - 1 - row;
                int symCol = size - 1 - col;
                
                // Only add each pair once (when current position is "before" symmetrical position)
                if (row < symRow || (row == symRow && col < symCol))
                {
                    if (puzzle[row, col] != 0 && puzzle[symRow, symCol] != 0)
                    {
                        pairs.Add(((row, col), (symRow, symCol)));
                    }
                }
            }
        }
        
        return pairs;
    }
    
    /// <summary>
    /// Checks if the puzzle has 180-degree rotational symmetry in clue positions.
    /// </summary>
    public static bool HasRotationalSymmetry(Board puzzle)
    {
        var size = puzzle.Size;
        
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                int symRow = size - 1 - row;
                int symCol = size - 1 - col;
                
                bool hasClue = puzzle[row, col] != 0;
                bool symHasClue = puzzle[symRow, symCol] != 0;
                
                if (hasClue != symHasClue)
                    return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets the number of candidates for an empty cell.
    /// Cells with more candidates are less constrained.
    /// </summary>
    public static int GetCandidateCount(Board puzzle, int row, int col)
    {
        if (puzzle[row, col] != 0)
            return 0;
        
        int count = 0;
        var size = puzzle.Size;
        
        for (int value = 1; value <= size; value++)
        {
            if (IsValidPlacement(puzzle, row, col, value))
            {
                count++;
            }
        }
        
        return count;
    }
    
    /// <summary>
    /// Gets candidate counts for all empty cells, sorted by count (ascending).
    /// Cells with fewer candidates are more constrained.
    /// </summary>
    public static List<(int row, int col, int candidateCount)> GetEmptyCellsByCandidateCount(Board puzzle)
    {
        var cells = new List<(int row, int col, int candidateCount)>();
        
        for (int row = 0; row < puzzle.Size; row++)
        {
            for (int col = 0; col < puzzle.Size; col++)
            {
                if (puzzle[row, col] == 0)
                {
                    var count = GetCandidateCount(puzzle, row, col);
                    cells.Add((row, col, count));
                }
            }
        }
        
        return cells.OrderBy(c => c.candidateCount).ToList();
    }
    
    /// <summary>
    /// Finds clues in over-constrained regions (good candidates for removal).
    /// </summary>
    public static List<(int row, int col)> GetCluesInOverConstrainedRegions(Board puzzle)
    {
        var distribution = ClueDistribution.FromBoard(puzzle);
        var clues = new List<(int row, int col)>();
        
        for (int row = 0; row < puzzle.Size; row++)
        {
            for (int col = 0; col < puzzle.Size; col++)
            {
                if (puzzle[row, col] != 0)
                {
                    var boxIndex = puzzle.GetBoxIndex(row, col);
                    
                    if (distribution.IsOverConstrained("Row", row) ||
                        distribution.IsOverConstrained("Column", col) ||
                        distribution.IsOverConstrained("Box", boxIndex))
                    {
                        clues.Add((row, col));
                    }
                }
            }
        }
        
        return clues;
    }
    
    /// <summary>
    /// Finds empty cells in under-constrained regions (good candidates for adding clues).
    /// </summary>
    public static List<(int row, int col)> GetEmptyCellsInUnderConstrainedRegions(Board puzzle)
    {
        var distribution = ClueDistribution.FromBoard(puzzle);
        var cells = new List<(int row, int col)>();
        
        for (int row = 0; row < puzzle.Size; row++)
        {
            for (int col = 0; col < puzzle.Size; col++)
            {
                if (puzzle[row, col] == 0)
                {
                    var boxIndex = puzzle.GetBoxIndex(row, col);
                    
                    if (distribution.IsUnderConstrained("Row", row) ||
                        distribution.IsUnderConstrained("Column", col) ||
                        distribution.IsUnderConstrained("Box", boxIndex))
                    {
                        cells.Add((row, col));
                    }
                }
            }
        }
        
        return cells;
    }
    
    private static bool IsValidPlacement(Board board, int row, int col, int value)
    {
        // Check row
        for (int c = 0; c < board.Size; c++)
        {
            if (board[row, c] == value)
                return false;
        }
        
        // Check column
        for (int r = 0; r < board.Size; r++)
        {
            if (board[r, col] == value)
                return false;
        }
        
        // Check box
        var boxIndex = board.GetBoxIndex(row, col);
        foreach (var (r, c, v) in board.GetBoxCells(boxIndex))
        {
            if (v == value)
                return false;
        }
        
        return true;
    }
}

