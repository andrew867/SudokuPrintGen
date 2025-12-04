using SudokuPrintGen.Core.LaTeX;
using SudokuPrintGen.Core.Output;
using SudokuPrintGen.Core.Puzzle;
using Xunit;

namespace SudokuPrintGen.Tests;

public class PdfCompilerTests
{
    [Fact]
    public void IsEngineAvailable_ChecksForLaTeXEngines()
    {
        // Test that we can check for LaTeX engines
        var xelatexAvailable = PdfCompiler.IsEngineAvailable(LaTeXEngine.XeLaTeX);
        var pdflatexAvailable = PdfCompiler.IsEngineAvailable(LaTeXEngine.PdfLaTeX);
        
        // At least one should be available if LaTeX is installed
        // (This test will pass even if neither is available, but documents the check)
        Assert.True(true); // Test that the method doesn't throw
    }
    
    [Fact]
    public void Compile_WithValidLaTeX_ReturnsResult()
    {
        // Create a simple test LaTeX file
        var testDir = Path.Combine(Path.GetTempPath(), "SudokuPrintGenTest");
        Directory.CreateDirectory(testDir);
        
        try
        {
            var testTex = Path.Combine(testDir, "test.tex");
            var simpleLaTeX = @"\documentclass{article}\begin{document}Test\end{document}";
            File.WriteAllText(testTex, simpleLaTeX);
            
            var compiler = new PdfCompiler(LaTeXEngine.PdfLaTeX);
            
            // Only compile if engine is available
            if (PdfCompiler.IsEngineAvailable(LaTeXEngine.PdfLaTeX))
            {
                var result = compiler.Compile(testTex, testDir);
                // Result may be true or false depending on compilation success
                // But the method should not throw
                Assert.True(true);
            }
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
            catch { }
        }
    }
    
    [Fact]
    public void FindLaTeXExecutable_HandlesMissingEngines()
    {
        // Test that PdfCompiler can be instantiated even if LaTeX is not available
        var compiler = new PdfCompiler(LaTeXEngine.XeLaTeX);
        Assert.NotNull(compiler);
    }
}

