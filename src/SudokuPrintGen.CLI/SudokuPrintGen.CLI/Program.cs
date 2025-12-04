using SudokuPrintGen.Core.Puzzle;
using SudokuPrintGen.Core.Output;
using SudokuPrintGen.Core.LaTeX;
using SudokuPrintGen.Core.Configuration;
using System.Text.Json;

if (args.Length == 0 || args[0] != "generate")
{
    Console.WriteLine("SudokuPrintGen - Enterprise-grade LaTeX Sudoku generator");
    Console.WriteLine("\nUsage: sudoku-printgen generate [options]");
    Console.WriteLine("\nOptions:");
    Console.WriteLine("  --size, -s <int>          Board size (4, 6, 9, 12, 16) [default: 9]");
    Console.WriteLine("  --difficulty, -d <level>  Easy|Medium|Hard|Expert|Evil [default: Medium]");
    Console.WriteLine("  --variant, -v <type>      Classic|Diagonal|ColorConstrained|Kikagaku [default: Classic]");
    Console.WriteLine("  --count, -c <int>         Number of puzzles to generate [default: 1]");
    Console.WriteLine("  --output, -o <path>       Output directory [default: .]");
    Console.WriteLine("  --format, -f <type>       Tex|Txt|Pdf|Json|All [default: All]");
    Console.WriteLine("  --engine <engine>         pdflatex|xelatex [default: xelatex]");
    Console.WriteLine("  --font <path>             Path to TTF font file (xelatex only)");
    Console.WriteLine("  --system-font <name>      Use installed system font by name (xelatex only)");
    Console.WriteLine("  --no-bundled-font         Don't use bundled Futura Bold BT font");
    Console.WriteLine("  --title <text>            Puzzle title");
    Console.WriteLine("  --author <text>           Author name");
    Console.WriteLine("  --seed <int>              Random seed for reproducibility");
    Console.WriteLine("  --solution                Include solution in output");
    Console.WriteLine("  --solving-sheet           Include solving sheet (empty grid)");
    Console.WriteLine("  --puzzles-per-page <int>  Puzzles per page: 6 (larger, 2x3) or 8 (2x4) [default: 6]");
    Console.WriteLine("  --config <path>           Configuration file (JSON)");
    Console.WriteLine();
    Console.WriteLine("Difficulty Targeting Options:");
    Console.WriteLine("  --refine-difficulty       Use iterative refinement for accurate difficulty targeting");
    Console.WriteLine("  --show-statistics         Display generation statistics after completion");
    Console.WriteLine("  --verbose                 Show detailed progress during generation");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  sudoku-printgen generate -d Easy,Medium -c 8");
    Console.WriteLine("  sudoku-printgen generate -d Hard --refine-difficulty --show-statistics");
    Console.WriteLine("  sudoku-printgen generate -d Expert -c 4 --solution");
    return 1;
}

// Load configuration file if specified
var configPath = GetArgValueString(args, new[] { "--config" }, (string?)null);
GeneratorConfig? config = null;
if (configPath != null && File.Exists(configPath))
{
    try
    {
        var configJson = await File.ReadAllTextAsync(configPath);
        config = JsonSerializer.Deserialize<GeneratorConfig>(configJson);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to load config file: {ex.Message}");
    }
}

// Parse arguments (config file values as defaults, CLI args override)
var size = GetArgValue(args, new[] { "--size", "-s" }, config?.Size ?? 9);
var difficultyStr = GetArgValue(args, new[] { "--difficulty", "-d" }, config?.Difficulty ?? "Medium");
var variantStr = GetArgValue(args, new[] { "--variant", "-v" }, config?.Variant ?? "Classic");
var count = GetArgValue(args, new[] { "--count", "-c", "-n" }, config?.Count ?? 1);

// Parse multiple difficulties
var difficulties = DifficultyDistributor.ParseDifficulties(difficultyStr);
var distributedDifficulties = DifficultyDistributor.DistributeDifficulties(difficulties, count);
var outputDir = GetArgValue(args, new[] { "--output", "-o" }, config?.Output ?? ".");
var format = GetArgValue(args, new[] { "--format", "-f" }, config?.Format ?? "All");
var engine = GetArgValue(args, new[] { "--engine" }, config?.Engine ?? "xelatex");
var fontPath = GetArgValueString(args, new[] { "--font" }, config?.Font);
var systemFont = GetArgValueString(args, new[] { "--system-font" }, null);
var useBundledFont = !HasArg(args, new[] { "--no-bundled-font" });
var title = GetArgValueString(args, new[] { "--title" }, config?.Title);
var author = GetArgValueString(args, new[] { "--author" }, config?.Author);
var seedStr = GetArgValueString(args, new[] { "--seed" }, config?.Seed?.ToString());
var includeSolution = HasArg(args, new[] { "--solution", "--include-solution" }) || (config?.IncludeSolution ?? false);
var includeSolvingSheet = HasArg(args, new[] { "--solving-sheet" }) || (config?.IncludeSolvingSheet ?? false);
var puzzlesPerPage = GetArgValue(args, new[] { "--puzzles-per-page" }, 6);

// Difficulty refinement options
var useIterativeRefinement = HasArg(args, new[] { "--refine-difficulty", "--refine", "--iterative" });
var showStatistics = HasArg(args, new[] { "--show-statistics", "--stats", "--statistics" });
var verbose = HasArg(args, new[] { "--verbose", "-V" });

// Parse enums (for backward compatibility, but we'll use distributedDifficulties)
var defaultDifficulty = difficulties.Count > 0 ? difficulties[0] : Difficulty.Medium;

if (!Enum.TryParse<Variant>(variantStr, true, out var variant))
{
    variant = Variant.Classic;
}

int? seed = null;
if (seedStr != null && int.TryParse(seedStr, out var seedValue))
{
    seed = seedValue;
}
else if (seedStr == null)
{
    // Generate random seed and show it to user
    seed = new Random().Next();
    Console.WriteLine($"Using random seed: {seed}");
}

// Determine box dimensions
var (boxRows, boxCols) = GetBoxDimensions(size);

// Create generator with statistics tracking
var generator = new PuzzleGenerator(seed: seed);

if (useIterativeRefinement)
{
    Console.WriteLine("Using iterative difficulty refinement for accurate targeting...");
}

// Create style options
var styleOptions = new LaTeXStyleOptions
{
    Engine = engine == "xelatex" ? LaTeXEngine.XeLaTeX : LaTeXEngine.PdfLaTeX,
    FontPath = fontPath,                    // Custom TTF font file path
    FontFamily = systemFont,                // System-installed font name
    UseBundledFont = useBundledFont && string.IsNullOrEmpty(fontPath) && string.IsNullOrEmpty(systemFont),
    Title = title ?? "Sudoku Puzzle",
    Author = author ?? "SudokuPrintGen",
    IncludeSolution = includeSolution,
    IncludeSolvingSheet = includeSolvingSheet,
    PuzzlesPerPage = puzzlesPerPage
};

var formatWriter = new FormatWriter(styleOptions);
var pdfCompiler = new PdfCompiler(styleOptions.Engine);

// Ensure output directory exists
Directory.CreateDirectory(outputDir);

// Generate all puzzles first with distributed difficulties
var allPuzzles = new List<GeneratedPuzzle>();
var generationTimestamp = DateTime.UtcNow;
var baseSeed = seed ?? new Random().Next();

Console.WriteLine($"\nGenerating {count} puzzle(s)...\n");

for (int i = 0; i < count; i++)
{
    // Use different seed for each puzzle (increment base seed)
    int? puzzleSeed = seed.HasValue ? baseSeed + i : null;
    var puzzleGenerator = new PuzzleGenerator(seed: puzzleSeed);
    
    // Use distributed difficulty for this puzzle
    var puzzleDifficulty = distributedDifficulties[i];
    
    if (verbose)
    {
        Console.WriteLine($"Generating puzzle {i + 1}/{count} (Difficulty: {puzzleDifficulty})...");
    }
    
    var generatedPuzzle = puzzleGenerator.Generate(
        puzzleDifficulty, 
        variant, 
        size, 
        boxRows, 
        boxCols, 
        useIterativeRefinement: useIterativeRefinement
    );
    generatedPuzzle.PuzzleNumber = i + 1;
    generatedPuzzle.GeneratedAt = generationTimestamp; // Use same timestamp for all puzzles in batch
    
    // Add to main generator's statistics
    generator.Statistics.AddPuzzle(generatedPuzzle);
    
    allPuzzles.Add(generatedPuzzle);
    
    if (verbose && generatedPuzzle.DifficultyRating != null)
    {
        var rating = generatedPuzzle.DifficultyRating;
        Console.WriteLine($"  Iterations: {rating.IterationCount}, Score: {rating.CompositeScore:F1}, " +
                          $"Estimated: {rating.EstimatedDifficulty}, Match: {(rating.IsInTargetRange ? "Yes" : "No")}");
    }
}

// Normalize format to handle various inputs (case-insensitive, aliases)
var formatLower = format.ToLowerInvariant();
var wantsTex = formatLower == "all" || formatLower.Contains("tex") || formatLower.Contains("latex");
var wantsPdf = formatLower == "all" || formatLower.Contains("pdf");
var wantsTxt = formatLower == "all" || formatLower.Contains("txt") || formatLower.Contains("text");
var wantsJson = formatLower == "all" || formatLower.Contains("json");

// Write individual formats (TXT, JSON)
for (int i = 0; i < allPuzzles.Count; i++)
{
    var generatedPuzzle = allPuzzles[i];
    var puzzleId = generatedPuzzle.Seed.HasValue 
        ? $"seed_{generatedPuzzle.Seed.Value}" 
        : $"puzzle_{i + 1:D3}";
    var baseName = $"sudoku_{generatedPuzzle.Difficulty}_{puzzleId}";
    
    if (wantsTxt)
    {
        var txtContent = formatWriter.WriteText(generatedPuzzle.Puzzle);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"{baseName}.txt"), txtContent);
    }
    
    if (wantsJson)
    {
        var jsonContent = formatWriter.WriteJson(
            generatedPuzzle.Puzzle,
            includeSolution ? generatedPuzzle.Solution : null,
            generatedPuzzle
        );
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"{baseName}.json"), jsonContent);
    }
    
    Console.WriteLine($"Generated: {baseName}");
}

// Write combined LaTeX document if multiple puzzles
if (wantsTex || wantsPdf)
{
    string latexContent;
    string baseName;
    
    if (count > 1)
    {
        // Generate combined LaTeX document with multiple puzzles
        var puzzles = allPuzzles.Select(p => (
            p.Puzzle,
            includeSolution ? p.Solution : null,
            (GeneratedPuzzle?)p
        )).ToList();
        
        var latexGen = new LaTeXGenerator(styleOptions);
        latexContent = latexGen.GenerateMultiplePuzzles(puzzles);
        
        // Use timestamp-based naming for combined files
        var timestamp = generationTimestamp.ToString("yyyyMMdd_HHmmss");
        baseName = $"sudoku_combined_{timestamp}";
    }
    else
    {
        // Single puzzle
        var generatedPuzzle = allPuzzles[0];
        latexContent = formatWriter.WriteLaTeX(
            generatedPuzzle.Puzzle,
            includeSolution ? generatedPuzzle.Solution : null,
            generatedPuzzle
        );
        var puzzleId = generatedPuzzle.Seed.HasValue 
            ? $"seed_{generatedPuzzle.Seed.Value}" 
            : "puzzle_001";
        baseName = $"sudoku_{generatedPuzzle.Difficulty}_{puzzleId}";
    }
    
    var texPath = Path.Combine(outputDir, $"{baseName}.tex");
    await File.WriteAllTextAsync(texPath, latexContent);
    Console.WriteLine($"Generated LaTeX: {baseName}.tex");
    
    if (wantsPdf)
    {
        if (PdfCompiler.IsEngineAvailable(styleOptions.Engine))
        {
            pdfCompiler.Compile(texPath, outputDir);
            Console.WriteLine($"Generated PDF: {baseName}.pdf");
        }
        else
        {
            Console.WriteLine($"Warning: {engine} not found. PDF compilation skipped.");
        }
    }
}

Console.WriteLine($"\nGenerated {count} puzzle(s) in {outputDir}");

// Show statistics if requested
if (showStatistics)
{
    Console.WriteLine();
    Console.WriteLine(generator.GetStatisticsReport());
}

return 0;

static (int boxRows, int boxCols) GetBoxDimensions(int size)
{
    return size switch
    {
        4 => (2, 2),
        6 => (2, 3),
        9 => (3, 3),
        12 => (3, 4),
        16 => (4, 4),
        _ => (3, 3) // Default to 3x3
    };
}

static T GetArgValue<T>(string[] args, string[] names, T defaultValue)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (names.Contains(args[i]))
        {
            var value = args[i + 1];
            if (typeof(T) == typeof(int) && int.TryParse(value, out var intVal))
            {
                return (T)(object)intVal;
            }
            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }
        }
    }
    return defaultValue;
}

static string? GetArgValueString(string[] args, string[] names, string? defaultValue)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (names.Contains(args[i]))
        {
            return args[i + 1];
        }
    }
    return defaultValue;
}

static bool HasArg(string[] args, string[] names)
{
    return args.Any(names.Contains);
}
