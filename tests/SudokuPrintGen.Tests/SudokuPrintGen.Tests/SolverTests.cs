using SudokuPrintGen.Core.Puzzle;
using SudokuPrintGen.Core.Solver;
using Xunit;

namespace SudokuPrintGen.Tests;

public class SolverTests
{
    [Fact]
    public void Solve_ValidPuzzle_ReturnsSolution()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        var solver = new DpllSolver();
        
        var solution = solver.Solve(puzzle);
        
        Assert.NotNull(solution);
        Assert.True(solution!.IsComplete());
    }
    
    [Fact]
    public void HasUniqueSolution_ValidPuzzle_ReturnsTrue()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        var solver = new DpllSolver();
        
        var hasUnique = solver.HasUniqueSolution(puzzle);
        
        Assert.True(hasUnique);
    }
    
    [Fact]
    public void CountSolutions_ValidPuzzle_ReturnsOne()
    {
        var puzzle = Board.FromString("530070000600195000098000060800060003400803001700020006060000280000419005000080079");
        var solver = new DpllSolver();
        
        var count = solver.CountSolutions(puzzle, 2);
        
        Assert.Equal(1, count);
    }
}

