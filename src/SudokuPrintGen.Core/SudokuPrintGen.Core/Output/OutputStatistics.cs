namespace SudokuPrintGen.Core.Output;

/// <summary>
/// Statistics about puzzle generation and solving.
/// </summary>
public class OutputStatistics
{
    public TimeSpan GenerationTime { get; set; }
    public TimeSpan SolveTime { get; set; }
    public int GuessCount { get; set; }
    public int ClueCount { get; set; }
    public int Attempts { get; set; }
}

