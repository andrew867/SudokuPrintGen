using SudokuPrintGen.Core.Puzzle;
using System.Text.Json.Serialization;

namespace SudokuPrintGen.Core.Configuration;

/// <summary>
/// Configuration for puzzle generation.
/// </summary>
public class GeneratorConfig
{
    [JsonPropertyName("size")]
    public int Size { get; set; } = 9;
    
    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "Medium";
    
    [JsonPropertyName("variant")]
    public string Variant { get; set; } = "Classic";
    
    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;
    
    [JsonPropertyName("output")]
    public string? Output { get; set; }
    
    [JsonPropertyName("format")]
    public string Format { get; set; } = "All";
    
    [JsonPropertyName("engine")]
    public string Engine { get; set; } = "xelatex";
    
    [JsonPropertyName("font")]
    public string? Font { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    
    [JsonPropertyName("seed")]
    public int? Seed { get; set; }
    
    [JsonPropertyName("includeSolution")]
    public bool IncludeSolution { get; set; } = false;
    
    [JsonPropertyName("includeSolvingSheet")]
    public bool IncludeSolvingSheet { get; set; } = false;
}

