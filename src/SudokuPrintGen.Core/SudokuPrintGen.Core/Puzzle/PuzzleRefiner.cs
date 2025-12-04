using SudokuPrintGen.Core.Solver;

namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Provides methods for refining puzzle difficulty through strategic clue management.
/// </summary>
public static class PuzzleRefiner
{
    /// <summary>
    /// Maximum number of refinement attempts before giving up.
    /// </summary>
    public const int MaxRefinementIterations = 50;
    
    /// <summary>
    /// Acceptable relative deviation from target (15%).
    /// </summary>
    public const double RefinementTolerance = 0.15;
    
    /// <summary>
    /// Finds the optimal clue to remove to increase difficulty while maintaining uniqueness.
    /// Returns null if no suitable clue is found.
    /// </summary>
    public static (int row, int col)? FindOptimalClueToRemove(Board puzzle, Board solution, ISolver solver)
    {
        var originalMetrics = solver.SolveWithMetrics(puzzle);
        var bestCandidate = default((int row, int col)?);
        var bestDifficultyIncrease = 0.0;
        
        // Try removing each clue and measure impact
        for (int row = 0; row < puzzle.Size; row++)
        {
            for (int col = 0; col < puzzle.Size; col++)
            {
                if (puzzle[row, col] == 0)
                    continue;
                
                var testBoard = puzzle.Clone();
                testBoard[row, col] = 0;
                
                // Check if puzzle still has unique solution
                if (!solver.HasUniqueSolution(testBoard))
                    continue;
                
                // Measure difficulty increase
                var newMetrics = solver.SolveWithMetrics(testBoard);
                var difficultyIncrease = newMetrics.DifficultyScore - originalMetrics.DifficultyScore;
                
                // We want positive increase (harder puzzle)
                if (difficultyIncrease > bestDifficultyIncrease)
                {
                    bestDifficultyIncrease = difficultyIncrease;
                    bestCandidate = (row, col);
                }
            }
        }
        
        return bestCandidate;
    }
    
    /// <summary>
    /// Finds the optimal clue to add to decrease difficulty.
    /// Returns null if no suitable position is found.
    /// </summary>
    public static (int row, int col, int value)? FindOptimalClueToAdd(Board puzzle, Board solution, ISolver solver)
    {
        var originalMetrics = solver.SolveWithMetrics(puzzle);
        var bestCandidate = default((int row, int col, int value)?);
        var bestDifficultyDecrease = 0.0;
        
        // Try adding each possible clue and measure impact
        for (int row = 0; row < puzzle.Size; row++)
        {
            for (int col = 0; col < puzzle.Size; col++)
            {
                if (puzzle[row, col] != 0)
                    continue;
                
                var value = solution[row, col];
                var testBoard = puzzle.Clone();
                testBoard[row, col] = value;
                
                // Measure difficulty decrease
                var newMetrics = solver.SolveWithMetrics(testBoard);
                var difficultyDecrease = originalMetrics.DifficultyScore - newMetrics.DifficultyScore;
                
                // We want positive decrease (easier puzzle)
                if (difficultyDecrease > bestDifficultyDecrease)
                {
                    bestDifficultyDecrease = difficultyDecrease;
                    bestCandidate = (row, col, value);
                }
            }
        }
        
        return bestCandidate;
    }
    
    /// <summary>
    /// Estimates the difficulty impact of removing or adding a clue.
    /// Positive value means harder puzzle after the change.
    /// </summary>
    public static double EstimateDifficultyImpact(Board puzzle, int row, int col, bool isRemoval, ISolver solver, Board? solution = null)
    {
        var originalMetrics = solver.SolveWithMetrics(puzzle);
        var testBoard = puzzle.Clone();
        
        if (isRemoval)
        {
            if (puzzle[row, col] == 0)
                return 0.0; // Not a clue
            testBoard[row, col] = 0;
        }
        else
        {
            if (puzzle[row, col] != 0 || solution == null)
                return 0.0; // Already has a clue or no solution provided
            testBoard[row, col] = solution[row, col];
        }
        
        // Check uniqueness for removal
        if (isRemoval && !solver.HasUniqueSolution(testBoard))
        {
            return double.NegativeInfinity; // Invalid - breaks uniqueness
        }
        
        var newMetrics = solver.SolveWithMetrics(testBoard);
        return newMetrics.DifficultyScore - originalMetrics.DifficultyScore;
    }
    
    /// <summary>
    /// Increases puzzle difficulty by strategically removing clues.
    /// Maintains puzzle uniqueness and symmetry where possible.
    /// </summary>
    public static Board IncreaseDifficulty(Board puzzle, Board solution, ISolver solver, Random random)
    {
        var result = puzzle.Clone();
        var hasSymmetry = ClueAnalyzer.HasRotationalSymmetry(puzzle);
        
        // Strategy 1: Try clues in over-constrained regions first
        var overConstrainedClues = ClueAnalyzer.GetCluesInOverConstrainedRegions(result);
        ShuffleList(overConstrainedClues, random);
        
        foreach (var (row, col) in overConstrainedClues)
        {
            if (TryRemoveClue(result, row, col, solver, hasSymmetry))
            {
                return result;
            }
        }
        
        // Strategy 2: Get clues by importance and try removing least important
        var cluesByImportance = ClueAnalyzer.GetCluesByImportance(result, solution, solver);
        
        foreach (var (row, col, _) in cluesByImportance.Take(10))
        {
            if (TryRemoveClue(result, row, col, solver, hasSymmetry))
            {
                return result;
            }
        }
        
        // Strategy 3: Find optimal clue to remove
        var optimalToRemove = FindOptimalClueToRemove(result, solution, solver);
        if (optimalToRemove.HasValue)
        {
            var (row, col) = optimalToRemove.Value;
            if (TryRemoveClue(result, row, col, solver, hasSymmetry))
            {
                return result;
            }
        }
        
        return result; // No changes possible
    }
    
    /// <summary>
    /// Decreases puzzle difficulty by strategically adding clues.
    /// </summary>
    public static Board SimplifyPuzzle(Board puzzle, Board solution, ISolver solver, Random random)
    {
        var result = puzzle.Clone();
        var hasSymmetry = ClueAnalyzer.HasRotationalSymmetry(puzzle);
        
        // Strategy 1: Add clues to under-constrained regions first
        var underConstrainedCells = ClueAnalyzer.GetEmptyCellsInUnderConstrainedRegions(result);
        ShuffleList(underConstrainedCells, random);
        
        foreach (var (row, col) in underConstrainedCells)
        {
            AddClue(result, row, col, solution, hasSymmetry);
            return result;
        }
        
        // Strategy 2: Find optimal clue to add
        var optimalToAdd = FindOptimalClueToAdd(result, solution, solver);
        if (optimalToAdd.HasValue)
        {
            var (row, col, _) = optimalToAdd.Value;
            AddClue(result, row, col, solution, hasSymmetry);
            return result;
        }
        
        // Strategy 3: Get candidate positions by effectiveness
        var candidates = ClueAnalyzer.GetCandidateCluePositions(result, solution, solver);
        if (candidates.Count > 0)
        {
            var (row, col, _, _) = candidates[0];
            AddClue(result, row, col, solution, hasSymmetry);
            return result;
        }
        
        return result; // No changes possible
    }
    
    /// <summary>
    /// Refines a puzzle to match a target difficulty level.
    /// Returns the refined puzzle and whether it matches the target.
    /// </summary>
    public static (Board puzzle, bool success, int iterations, DifficultyRating finalRating) RefineToDifficulty(
        Board puzzle, 
        Board solution, 
        Difficulty targetDifficulty, 
        ISolver solver, 
        Random random)
    {
        var currentPuzzle = puzzle.Clone();
        var iterations = 0;
        DifficultyRating rating;
        
        while (iterations < MaxRefinementIterations)
        {
            iterations++;
            
            // Rate current puzzle
            rating = DifficultyRater.RatePuzzleWithMetrics(currentPuzzle, solver);
            var comparison = DifficultyTargets.CompareScoreToDifficulty(rating.CompositeScore, targetDifficulty);
            
            // Check if we're in range
            if (comparison == DifficultyComparison.InRange)
            {
                rating.TargetDifficulty = targetDifficulty;
                rating.IsInTargetRange = true;
                return (currentPuzzle, true, iterations, rating);
            }
            
            // Try to adjust difficulty
            Board newPuzzle;
            if (comparison == DifficultyComparison.TooEasy)
            {
                newPuzzle = IncreaseDifficulty(currentPuzzle, solution, solver, random);
            }
            else // TooHard
            {
                newPuzzle = SimplifyPuzzle(currentPuzzle, solution, solver, random);
            }
            
            // Check if we made progress
            if (BoardsEqual(newPuzzle, currentPuzzle))
            {
                // No change possible, break to avoid infinite loop
                break;
            }
            
            currentPuzzle = newPuzzle;
        }
        
        // Return best effort
        rating = DifficultyRater.RatePuzzleWithMetrics(currentPuzzle, solver);
        rating.TargetDifficulty = targetDifficulty;
        rating.IsInTargetRange = DifficultyTargets.IsScoreInRange(rating.CompositeScore, targetDifficulty);
        
        return (currentPuzzle, rating.IsInTargetRange, iterations, rating);
    }
    
    private static bool TryRemoveClue(Board puzzle, int row, int col, ISolver solver, bool maintainSymmetry)
    {
        if (puzzle[row, col] == 0)
            return false;
        
        var testBoard = puzzle.Clone();
        testBoard[row, col] = 0;
        
        // If maintaining symmetry, also remove symmetrical clue
        int symRow = puzzle.Size - 1 - row;
        int symCol = puzzle.Size - 1 - col;
        bool removedSymmetrical = false;
        
        if (maintainSymmetry && (row != symRow || col != symCol))
        {
            if (testBoard[symRow, symCol] != 0)
            {
                testBoard[symRow, symCol] = 0;
                removedSymmetrical = true;
            }
        }
        
        // Check if puzzle still has unique solution
        if (solver.HasUniqueSolution(testBoard))
        {
            puzzle[row, col] = 0;
            if (removedSymmetrical)
            {
                puzzle[symRow, symCol] = 0;
            }
            return true;
        }
        
        return false;
    }
    
    private static void AddClue(Board puzzle, int row, int col, Board solution, bool maintainSymmetry)
    {
        if (puzzle[row, col] != 0)
            return;
        
        puzzle[row, col] = solution[row, col];
        
        // If maintaining symmetry, also add symmetrical clue
        if (maintainSymmetry)
        {
            int symRow = puzzle.Size - 1 - row;
            int symCol = puzzle.Size - 1 - col;
            
            if ((row != symRow || col != symCol) && puzzle[symRow, symCol] == 0)
            {
                puzzle[symRow, symCol] = solution[symRow, symCol];
            }
        }
    }
    
    private static bool BoardsEqual(Board a, Board b)
    {
        if (a.Size != b.Size)
            return false;
        
        for (int row = 0; row < a.Size; row++)
        {
            for (int col = 0; col < a.Size; col++)
            {
                if (a[row, col] != b[row, col])
                    return false;
            }
        }
        
        return true;
    }
    
    private static void ShuffleList<T>(List<T> list, Random random)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}

