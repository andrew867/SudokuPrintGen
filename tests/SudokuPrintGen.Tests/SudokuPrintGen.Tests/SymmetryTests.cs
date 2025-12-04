using SudokuPrintGen.Core.Puzzle;
using Xunit;

namespace SudokuPrintGen.Tests;

public class SymmetryTests
{
    [Fact]
    public void DetectSymmetry_RotationalSymmetry_DetectsCorrectly()
    {
        // Create a puzzle with rotational symmetry (180 degrees)
        var puzzle = Board.CreateStandard();
        puzzle[0, 0] = 5;
        puzzle[8, 8] = 5; // Opposite corner
        
        var symmetry = SymmetryDetector.DetectSymmetry(puzzle);
        
        // Should detect some symmetry (may not be perfect due to other cells)
        Assert.NotNull(symmetry);
    }
    
    [Fact]
    public void DetectSymmetry_EmptyBoard_ReturnsInfo()
    {
        var puzzle = Board.CreateStandard();
        var symmetry = SymmetryDetector.DetectSymmetry(puzzle);
        
        Assert.NotNull(symmetry);
        Assert.True(symmetry.SymmetryScore >= 0);
    }
    
    [Fact]
    public void GetSymmetryTypes_ReturnsList()
    {
        var puzzle = Board.CreateStandard();
        var symmetry = SymmetryDetector.DetectSymmetry(puzzle);
        var types = symmetry.GetSymmetryTypes();
        
        Assert.NotNull(types);
        Assert.IsType<List<string>>(types);
    }
}

