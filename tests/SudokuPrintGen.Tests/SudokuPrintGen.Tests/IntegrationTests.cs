using SudokuPrintGen.Core.Puzzle;
using SudokuPrintGen.Core.Output;
using SudokuPrintGen.Core.LaTeX;
using Xunit;

namespace SudokuPrintGen.Tests;

public class IntegrationTests
{
    [Fact]
    public void GenerateAndValidate_ProducesValidPuzzle()
    {
        var generator = new PuzzleGenerator(seed: 123);
        var puzzle = generator.Generate(Difficulty.Medium);
        
        var validation = BoardValidator.Validate(puzzle.Puzzle);
        Assert.True(validation.IsValid, validation.GetErrorMessage());
    }
    
    [Fact]
    public void FormatWriter_GeneratesValidJson()
    {
        var generator = new PuzzleGenerator(seed: 456);
        var generatedPuzzle = generator.Generate(Difficulty.Easy);
        var writer = new FormatWriter();
        
        var json = writer.WriteJson(generatedPuzzle.Puzzle, generatedPuzzle.Solution, generatedPuzzle);
        
        Assert.NotNull(json);
        Assert.Contains("\"puzzle\"", json);
        Assert.Contains("\"difficulty\"", json);
        Assert.Contains("\"seed\"", json);
    }
    
    [Fact]
    public void FormatWriter_GeneratesValidLaTeX()
    {
        var generator = new PuzzleGenerator(seed: 789);
        var generatedPuzzle = generator.Generate(Difficulty.Hard);
        var writer = new FormatWriter();
        
        var latex = writer.WriteLaTeX(generatedPuzzle.Puzzle, generatedPuzzle.Solution, generatedPuzzle);
        
        Assert.NotNull(latex);
        Assert.Contains("\\documentclass", latex);
        Assert.Contains("\\begin{document}", latex);
        Assert.Contains("\\end{document}", latex);
    }
    
    [Fact]
    public void LaTeXGenerator_IncludesMetadata()
    {
        var generator = new PuzzleGenerator(seed: 999);
        var generatedPuzzle = generator.Generate(Difficulty.Expert);
        generatedPuzzle.PuzzleNumber = 42;
        
        var latexGen = new LaTeXGenerator();
        var latex = latexGen.Generate(generatedPuzzle.Puzzle, generatedPuzzle.Solution, generatedPuzzle);
        
        Assert.Contains("Difficulty:", latex);
        Assert.Contains("Seed:", latex);
        Assert.Contains(@"Puzzle \#", latex);
        Assert.Contains("Generated:", latex);
        Assert.Contains("Solver:", latex);
    }
    
    [Fact]
    public void LaTeXGenerator_IncludesSolvingSheet_WhenRequested()
    {
        var options = new LaTeXStyleOptions { IncludeSolvingSheet = true };
        var generator = new PuzzleGenerator(seed: 111);
        var generatedPuzzle = generator.Generate(Difficulty.Medium);
        
        var latexGen = new LaTeXGenerator(options);
        var latex = latexGen.Generate(generatedPuzzle.Puzzle, null, generatedPuzzle);
        
        Assert.Contains("Solving Sheet", latex);
        Assert.Contains("\\newpage", latex);
    }
}

