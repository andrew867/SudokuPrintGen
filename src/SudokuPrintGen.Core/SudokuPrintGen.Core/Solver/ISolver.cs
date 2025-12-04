using SudokuPrintGen.Core.Puzzle;

namespace SudokuPrintGen.Core.Solver;

/// <summary>
/// Interface for Sudoku solvers.
/// </summary>
public interface ISolver
{
    /// <summary>
    /// Solves the given puzzle and returns the solution, or null if no solution exists.
    /// </summary>
    Board? Solve(Board puzzle);
    
    /// <summary>
    /// Counts the number of solutions (up to the limit).
    /// </summary>
    int CountSolutions(Board puzzle, int limit = 2);
    
    /// <summary>
    /// Checks if the puzzle has a unique solution.
    /// </summary>
    bool HasUniqueSolution(Board puzzle);
}

