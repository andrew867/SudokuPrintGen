using System.Diagnostics;
using System.Text;
using SudokuPrintGen.Core.LaTeX;

namespace SudokuPrintGen.Core.Output;

/// <summary>
/// Compiles LaTeX files to PDF.
/// </summary>
public class PdfCompiler
{
    private readonly string _latexEngine;
    private readonly string? _latexEnginePath;
    
    public PdfCompiler(LaTeXEngine engine = LaTeXEngine.XeLaTeX)
    {
        var engineName = engine == LaTeXEngine.XeLaTeX ? "xelatex" : "pdflatex";
        _latexEngine = engineName;
        _latexEnginePath = FindLaTeXExecutable(engineName);
    }
    
    /// <summary>
    /// Finds the LaTeX executable path, checking system PATH and MikTeX installation.
    /// </summary>
    private static string? FindLaTeXExecutable(string engineName)
    {
        // First, try to find in system PATH
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = engineName,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(processInfo);
            if (process != null)
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    return engineName; // Found in PATH
                }
            }
        }
        catch
        {
            // Not in PATH, continue to check MikTeX
        }
        
        // Check MikTeX installation directory
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var miktexPaths = new[]
        {
            Path.Combine(userProfile, "AppData", "Local", "Programs", "MiKTeX", "miktex", "bin", "x64", engineName + ".exe"),
            Path.Combine(userProfile, "AppData", "Local", "Programs", "MiKTeX", "miktex", "bin", engineName + ".exe"),
            Path.Combine(userProfile, "AppData", "Local", "Programs", "MiKTeX", "bin", "x64", engineName + ".exe"),
            Path.Combine(userProfile, "AppData", "Local", "Programs", "MiKTeX", "bin", engineName + ".exe"),
            // Also check without .exe extension (for cross-platform compatibility)
            Path.Combine(userProfile, "AppData", "Local", "Programs", "MiKTeX", "miktex", "bin", "x64", engineName),
            Path.Combine(userProfile, "AppData", "Local", "Programs", "MiKTeX", "miktex", "bin", engineName),
            Path.Combine(userProfile, "AppData", "Local", "Programs", "MiKTeX", "bin", "x64", engineName),
            Path.Combine(userProfile, "AppData", "Local", "Programs", "MiKTeX", "bin", engineName)
        };
        
        foreach (var path in miktexPaths)
        {
            if (File.Exists(path))
            {
                // Verify it's actually executable
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    using var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        process.WaitForExit();
                        if (process.ExitCode == 0)
                        {
                            return path;
                        }
                    }
                }
                catch
                {
                    // Continue to next path
                }
            }
        }
        
        return null; // Not found
    }
    
    /// <summary>
    /// Compiles a LaTeX file to PDF.
    /// </summary>
    public bool Compile(string latexFilePath, string? outputDirectory = null)
    {
        // Get full path to the LaTeX file
        var fullLatexPath = Path.GetFullPath(latexFilePath);
        var directory = outputDirectory != null ? Path.GetFullPath(outputDirectory) : (Path.GetDirectoryName(fullLatexPath) ?? ".");
        var fileName = Path.GetFileNameWithoutExtension(fullLatexPath);
        var justFileName = Path.GetFileName(fullLatexPath);
        
        // Use found path or fall back to engine name
        var enginePath = _latexEnginePath ?? _latexEngine;
        
        try
        {
            // Use just the filename since we're setting working directory to the file's location
            var fileDir = Path.GetDirectoryName(fullLatexPath) ?? ".";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = enginePath,
                // Use just the filename, with output-directory for where PDF goes
                Arguments = $"-interaction=nonstopmode -output-directory=\"{directory}\" \"{justFileName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = fileDir // Work from the directory containing the .tex file
            };
            
            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return false;
            }
            
            // Capture output for debugging
            var output = new StringBuilder();
            var error = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) => {
                if (e.Data != null)
                {
                    output.AppendLine(e.Data);
                    // Output to console for debugging
                    Console.WriteLine($"[LaTeX] {e.Data}");
                }
            };
            process.ErrorDataReceived += (sender, e) => {
                if (e.Data != null)
                {
                    error.AppendLine(e.Data);
                    // Output errors to console
                    Console.Error.WriteLine($"[LaTeX Error] {e.Data}");
                }
            };
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            process.WaitForExit();
            
            // Log errors if compilation failed
            if (process.ExitCode != 0)
            {
                var outputText = output.ToString();
                var errorText = error.ToString();
                
                // Write error to a log file for debugging
                var logPath = Path.Combine(directory, fileName + "_compile.log");
                var logContent = $"Exit Code: {process.ExitCode}\n\nSTDOUT:\n{outputText}\n\nSTDERR:\n{errorText}";
                File.WriteAllText(logPath, logContent);
                
                // Also output to console
                Console.WriteLine($"\nLaTeX compilation failed (exit code: {process.ExitCode})");
                Console.WriteLine($"Full output saved to: {logPath}");
                if (!string.IsNullOrWhiteSpace(errorText))
                {
                    Console.WriteLine("\nError output:");
                    Console.WriteLine(errorText);
                }
                
                return false;
            }
            
            // Output success message
            Console.WriteLine($"LaTeX compilation successful: {fileName}.pdf");
            
            // Check if PDF was created
            var pdfPath = Path.Combine(directory, fileName + ".pdf");
            return File.Exists(pdfPath);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Checks if LaTeX engine is available.
    /// </summary>
    public static bool IsEngineAvailable(LaTeXEngine engine)
    {
        var engineName = engine == LaTeXEngine.XeLaTeX ? "xelatex" : "pdflatex";
        return FindLaTeXExecutable(engineName) != null;
    }
}


