using SudokuPrintGen.Core.Puzzle;
using Xunit;

namespace SudokuPrintGen.Tests;

public class GeneratorTests
{
    [Fact]
    public void Generate_WithSeed_ProducesReproduciblePuzzle()
    {
        var generator1 = new PuzzleGenerator(seed: 42);
        var generator2 = new PuzzleGenerator(seed: 42);
        
        var puzzle1 = generator1.Generate(Difficulty.Easy);
        var puzzle2 = generator2.Generate(Difficulty.Easy);
        
        Assert.Equal(puzzle1.Seed, puzzle2.Seed);
        Assert.NotNull(puzzle1.Solution);
        Assert.NotNull(puzzle2.Solution);
    }
    
    [Fact]
    public void Generate_ProducesValidPuzzle()
    {
        var generator = new PuzzleGenerator(seed: 123);
        var puzzle = generator.Generate(Difficulty.Medium);
        
        Assert.NotNull(puzzle.Puzzle);
        Assert.NotNull(puzzle.Solution);
        Assert.True(puzzle.Puzzle.Size > 0);
        Assert.True(puzzle.Solution.IsComplete());
    }
    
    [Fact]
    public void Generate_SetsMetadata()
    {
        var generator = new PuzzleGenerator(seed: 456);
        var puzzle = generator.Generate(Difficulty.Hard);
        
        Assert.NotNull(puzzle.Seed);
        Assert.NotEqual(default(DateTime), puzzle.GeneratedAt);
        Assert.Equal("DPLL", puzzle.SolverAlgorithm);
    }
}

