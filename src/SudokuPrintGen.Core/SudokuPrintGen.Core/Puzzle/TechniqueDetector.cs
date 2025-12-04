using System.Numerics;

namespace SudokuPrintGen.Core.Puzzle;

/// <summary>
/// Enumeration of Sudoku solving techniques, ordered by difficulty.
/// </summary>
public enum SolvingTechnique
{
    /// <summary>A cell with only one candidate.</summary>
    NakedSingle = 1,
    
    /// <summary>A digit that can only go in one cell within a unit.</summary>
    HiddenSingle = 2,
    
    /// <summary>Two cells in a unit with identical 2-candidate sets.</summary>
    NakedPair = 4,
    
    /// <summary>Two digits that appear in exactly 2 cells within a unit.</summary>
    HiddenPair = 5,
    
    /// <summary>A digit in exactly 2 cells in 2 rows sharing 2 columns (or vice versa).</summary>
    XWing = 8,
    
    /// <summary>Three cells forming a pivot-wing pattern eliminating a candidate.</summary>
    XYWing = 10,
    
    /// <summary>Extension of X-Wing to 3 rows/columns.</summary>
    Swordfish = 12,
    
    /// <summary>Similar to XY-Wing but pivot has 3 candidates.</summary>
    XYZWing = 14
}

/// <summary>
/// Represents a detected instance of a solving technique.
/// </summary>
/// <param name="Technique">The type of technique detected.</param>
/// <param name="Row">Primary row involved (-1 if not applicable).</param>
/// <param name="Col">Primary column involved (-1 if not applicable).</param>
/// <param name="Description">Human-readable description of the technique instance.</param>
public record TechniqueInstance(SolvingTechnique Technique, int Row, int Col, string Description);

/// <summary>
/// Helper class for managing candidate grids using bit vectors.
/// </summary>
public class CandidateGrid
{
    private readonly uint[,] _candidates;
    private readonly int _size;
    
    /// <summary>
    /// Gets the size of the grid (e.g., 9 for standard Sudoku).
    /// </summary>
    public int Size => _size;
    
    /// <summary>
    /// Gets candidates for a cell as a bitmask.
    /// </summary>
    public uint this[int row, int col]
    {
        get => _candidates[row, col];
        set => _candidates[row, col] = value;
    }
    
    /// <summary>
    /// Creates a candidate grid from a board.
    /// </summary>
    public CandidateGrid(Board board)
    {
        _size = board.Size;
        _candidates = new uint[_size, _size];
        
        // Initialize with all candidates for 9x9
        uint allCandidates = (1u << _size) - 1;
        
        Span<uint> rowUsed = stackalloc uint[_size];
        Span<uint> colUsed = stackalloc uint[_size];
        Span<uint> boxUsed = stackalloc uint[_size];
        
        // First pass: mark used digits
        for (int row = 0; row < _size; row++)
        {
            for (int col = 0; col < _size; col++)
            {
                var value = board[row, col];
                if (value != 0)
                {
                    var bit = 1u << (value - 1);
                    rowUsed[row] |= bit;
                    colUsed[col] |= bit;
                    boxUsed[board.GetBoxIndex(row, col)] |= bit;
                }
            }
        }
        
        // Second pass: compute candidates for empty cells
        for (int row = 0; row < _size; row++)
        {
            for (int col = 0; col < _size; col++)
            {
                if (board[row, col] == 0)
                {
                    var boxIndex = board.GetBoxIndex(row, col);
                    _candidates[row, col] = allCandidates & ~rowUsed[row] & ~colUsed[col] & ~boxUsed[boxIndex];
                }
                else
                {
                    _candidates[row, col] = 0;
                }
            }
        }
    }
    
    /// <summary>
    /// Gets the count of candidates for a cell.
    /// </summary>
    public int GetCandidateCount(int row, int col)
    {
        return BitOperations.PopCount(_candidates[row, col]);
    }
    
    /// <summary>
    /// Gets candidates as a list of digits (1-9).
    /// </summary>
    public List<int> GetCandidateList(int row, int col)
    {
        var result = new List<int>();
        var bits = _candidates[row, col];
        for (int d = 1; d <= _size; d++)
        {
            if ((bits & (1u << (d - 1))) != 0)
            {
                result.Add(d);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Checks if a cell has a specific candidate.
    /// </summary>
    public bool HasCandidate(int row, int col, int digit)
    {
        return (_candidates[row, col] & (1u << (digit - 1))) != 0;
    }
}

/// <summary>
/// Detects solving techniques in Sudoku puzzles.
/// </summary>
public static class TechniqueDetector
{
    /// <summary>
    /// Detects all applicable techniques in the puzzle.
    /// </summary>
    public static List<TechniqueInstance> DetectAllTechniques(Board board)
    {
        var techniques = new List<TechniqueInstance>();
        var candidates = new CandidateGrid(board);
        
        // Basic techniques
        techniques.AddRange(DetectNakedSingles(board, candidates));
        techniques.AddRange(DetectHiddenSingles(board, candidates));
        
        // Pair techniques
        techniques.AddRange(DetectNakedPairs(board, candidates));
        techniques.AddRange(DetectHiddenPairs(board, candidates));
        
        // Fish patterns
        techniques.AddRange(DetectXWing(board, candidates));
        techniques.AddRange(DetectSwordfish(board, candidates));
        
        // Wing patterns
        techniques.AddRange(DetectXYWing(board, candidates));
        techniques.AddRange(DetectXYZWing(board, candidates));
        
        return techniques;
    }
    
    /// <summary>
    /// Gets the difficulty weight for a technique.
    /// </summary>
    public static int GetTechniqueWeight(SolvingTechnique technique)
    {
        return (int)technique;
    }
    
    /// <summary>
    /// Calculates the total technique score from detected techniques.
    /// </summary>
    public static double CalculateTechniqueScore(List<TechniqueInstance> techniques)
    {
        if (techniques.Count == 0)
            return 0;
        
        // Score based on hardest technique required plus small bonus for variety
        var maxWeight = techniques.Max(t => GetTechniqueWeight(t.Technique));
        var uniqueTechniques = techniques.Select(t => t.Technique).Distinct().Count();
        
        return maxWeight + (uniqueTechniques - 1) * 0.5;
    }
    
    #region Naked Singles
    
    /// <summary>
    /// Detects naked singles - cells with only one candidate.
    /// </summary>
    public static List<TechniqueInstance> DetectNakedSingles(Board board, CandidateGrid candidates)
    {
        var results = new List<TechniqueInstance>();
        var size = board.Size;
        
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (board[row, col] == 0 && candidates.GetCandidateCount(row, col) == 1)
                {
                    var digit = BitOperations.TrailingZeroCount(candidates[row, col]) + 1;
                    results.Add(new TechniqueInstance(
                        SolvingTechnique.NakedSingle,
                        row, col,
                        $"R{row + 1}C{col + 1} = {digit} (naked single)"));
                }
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Simple boolean check for naked singles existence.
    /// </summary>
    public static bool HasNakedSingles(Board board)
    {
        var candidates = new CandidateGrid(board);
        return DetectNakedSingles(board, candidates).Count > 0;
    }
    
    #endregion
    
    #region Hidden Singles
    
    /// <summary>
    /// Detects hidden singles - a digit that can only go in one cell within a unit.
    /// </summary>
    public static List<TechniqueInstance> DetectHiddenSingles(Board board, CandidateGrid candidates)
    {
        var results = new List<TechniqueInstance>();
        var size = board.Size;
        
        // Check rows
        for (int row = 0; row < size; row++)
        {
            for (int digit = 1; digit <= size; digit++)
            {
                var positions = new List<int>();
                for (int col = 0; col < size; col++)
                {
                    if (board[row, col] == 0 && candidates.HasCandidate(row, col, digit))
                    {
                        positions.Add(col);
                    }
                }
                
                if (positions.Count == 1)
                {
                    results.Add(new TechniqueInstance(
                        SolvingTechnique.HiddenSingle,
                        row, positions[0],
                        $"R{row + 1}C{positions[0] + 1} = {digit} (hidden single in row)"));
                }
            }
        }
        
        // Check columns
        for (int col = 0; col < size; col++)
        {
            for (int digit = 1; digit <= size; digit++)
            {
                var positions = new List<int>();
                for (int row = 0; row < size; row++)
                {
                    if (board[row, col] == 0 && candidates.HasCandidate(row, col, digit))
                    {
                        positions.Add(row);
                    }
                }
                
                if (positions.Count == 1)
                {
                    // Avoid duplicates - only add if not already found in row check
                    var existing = results.Find(r => r.Row == positions[0] && r.Col == col 
                        && r.Technique == SolvingTechnique.HiddenSingle);
                    if (existing == null)
                    {
                        results.Add(new TechniqueInstance(
                            SolvingTechnique.HiddenSingle,
                            positions[0], col,
                            $"R{positions[0] + 1}C{col + 1} = {digit} (hidden single in column)"));
                    }
                }
            }
        }
        
        // Check boxes
        for (int box = 0; box < size; box++)
        {
            var boxCells = board.GetBoxCells(box).ToList();
            
            for (int digit = 1; digit <= size; digit++)
            {
                var positions = new List<(int row, int col)>();
                foreach (var (row, col, value) in boxCells)
                {
                    if (value == 0 && candidates.HasCandidate(row, col, digit))
                    {
                        positions.Add((row, col));
                    }
                }
                
                if (positions.Count == 1)
                {
                    var (r, c) = positions[0];
                    var existing = results.Find(x => x.Row == r && x.Col == c 
                        && x.Technique == SolvingTechnique.HiddenSingle);
                    if (existing == null)
                    {
                        results.Add(new TechniqueInstance(
                            SolvingTechnique.HiddenSingle,
                            r, c,
                            $"R{r + 1}C{c + 1} = {digit} (hidden single in box)"));
                    }
                }
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Simple boolean check for hidden singles existence.
    /// </summary>
    public static bool HasHiddenSingles(Board board)
    {
        var candidates = new CandidateGrid(board);
        return DetectHiddenSingles(board, candidates).Count > 0;
    }
    
    #endregion
    
    #region Naked Pairs
    
    /// <summary>
    /// Detects naked pairs - two cells in a unit with identical 2-candidate sets.
    /// </summary>
    public static List<TechniqueInstance> DetectNakedPairs(Board board, CandidateGrid candidates)
    {
        var results = new List<TechniqueInstance>();
        var size = board.Size;
        
        // Check rows
        for (int row = 0; row < size; row++)
        {
            var pairs = FindNakedPairsInUnit(
                Enumerable.Range(0, size).Select(col => (row, col)).ToList(),
                candidates);
            
            foreach (var (cell1, cell2, cands) in pairs)
            {
                var digits = GetDigitsFromMask(cands);
                results.Add(new TechniqueInstance(
                    SolvingTechnique.NakedPair,
                    cell1.row, cell1.col,
                    $"Naked pair {{{string.Join(",", digits)}}} at R{cell1.row + 1}C{cell1.col + 1} and R{cell2.row + 1}C{cell2.col + 1} in row"));
            }
        }
        
        // Check columns
        for (int col = 0; col < size; col++)
        {
            var pairs = FindNakedPairsInUnit(
                Enumerable.Range(0, size).Select(row => (row, col)).ToList(),
                candidates);
            
            foreach (var (cell1, cell2, cands) in pairs)
            {
                // Avoid duplicates if same cells
                if (cell1.row == cell2.row) continue; // Would be row pair
                
                var digits = GetDigitsFromMask(cands);
                results.Add(new TechniqueInstance(
                    SolvingTechnique.NakedPair,
                    cell1.row, cell1.col,
                    $"Naked pair {{{string.Join(",", digits)}}} at R{cell1.row + 1}C{cell1.col + 1} and R{cell2.row + 1}C{cell2.col + 1} in column"));
            }
        }
        
        // Check boxes
        for (int box = 0; box < size; box++)
        {
            var boxCells = board.GetBoxCells(box)
                .Select(c => (c.row, c.col))
                .ToList();
            
            var pairs = FindNakedPairsInUnit(boxCells, candidates);
            
            foreach (var (cell1, cell2, cands) in pairs)
            {
                // Avoid duplicates if same row or column
                if (cell1.row == cell2.row || cell1.col == cell2.col) continue;
                
                var digits = GetDigitsFromMask(cands);
                results.Add(new TechniqueInstance(
                    SolvingTechnique.NakedPair,
                    cell1.row, cell1.col,
                    $"Naked pair {{{string.Join(",", digits)}}} at R{cell1.row + 1}C{cell1.col + 1} and R{cell2.row + 1}C{cell2.col + 1} in box"));
            }
        }
        
        return results;
    }
    
    private static List<((int row, int col) cell1, (int row, int col) cell2, uint candidates)> 
        FindNakedPairsInUnit(List<(int row, int col)> cells, CandidateGrid candidates)
    {
        var results = new List<((int, int), (int, int), uint)>();
        
        // Find cells with exactly 2 candidates
        var biValueCells = cells
            .Where(c => candidates.GetCandidateCount(c.row, c.col) == 2)
            .ToList();
        
        // Check all pairs of bivalue cells
        for (int i = 0; i < biValueCells.Count; i++)
        {
            for (int j = i + 1; j < biValueCells.Count; j++)
            {
                var cell1 = biValueCells[i];
                var cell2 = biValueCells[j];
                var cands1 = candidates[cell1.row, cell1.col];
                var cands2 = candidates[cell2.row, cell2.col];
                
                // If same candidates, it's a naked pair
                if (cands1 == cands2)
                {
                    // Check if this pair would eliminate candidates from other cells
                    bool useful = cells.Any(c => 
                        c != cell1 && c != cell2 && 
                        (candidates[c.row, c.col] & cands1) != 0);
                    
                    if (useful)
                    {
                        results.Add((cell1, cell2, cands1));
                    }
                }
            }
        }
        
        return results;
    }
    
    #endregion
    
    #region Hidden Pairs
    
    /// <summary>
    /// Detects hidden pairs - two digits that appear in exactly 2 cells within a unit.
    /// </summary>
    public static List<TechniqueInstance> DetectHiddenPairs(Board board, CandidateGrid candidates)
    {
        var results = new List<TechniqueInstance>();
        var size = board.Size;
        
        // Check rows
        for (int row = 0; row < size; row++)
        {
            var pairs = FindHiddenPairsInUnit(
                Enumerable.Range(0, size).Select(col => (row, col)).ToList(),
                candidates, size);
            
            foreach (var (digit1, digit2, cell1, cell2) in pairs)
            {
                results.Add(new TechniqueInstance(
                    SolvingTechnique.HiddenPair,
                    cell1.row, cell1.col,
                    $"Hidden pair {{{digit1},{digit2}}} at R{cell1.row + 1}C{cell1.col + 1} and R{cell2.row + 1}C{cell2.col + 1} in row"));
            }
        }
        
        // Check columns
        for (int col = 0; col < size; col++)
        {
            var pairs = FindHiddenPairsInUnit(
                Enumerable.Range(0, size).Select(row => (row, col)).ToList(),
                candidates, size);
            
            foreach (var (digit1, digit2, cell1, cell2) in pairs)
            {
                if (cell1.row == cell2.row) continue; // Would be row pair
                
                results.Add(new TechniqueInstance(
                    SolvingTechnique.HiddenPair,
                    cell1.row, cell1.col,
                    $"Hidden pair {{{digit1},{digit2}}} at R{cell1.row + 1}C{cell1.col + 1} and R{cell2.row + 1}C{cell2.col + 1} in column"));
            }
        }
        
        // Check boxes
        for (int box = 0; box < size; box++)
        {
            var boxCells = board.GetBoxCells(box)
                .Select(c => (c.row, c.col))
                .ToList();
            
            var pairs = FindHiddenPairsInUnit(boxCells, candidates, size);
            
            foreach (var (digit1, digit2, cell1, cell2) in pairs)
            {
                if (cell1.row == cell2.row || cell1.col == cell2.col) continue;
                
                results.Add(new TechniqueInstance(
                    SolvingTechnique.HiddenPair,
                    cell1.row, cell1.col,
                    $"Hidden pair {{{digit1},{digit2}}} at R{cell1.row + 1}C{cell1.col + 1} and R{cell2.row + 1}C{cell2.col + 1} in box"));
            }
        }
        
        return results;
    }
    
    private static List<(int digit1, int digit2, (int row, int col) cell1, (int row, int col) cell2)> 
        FindHiddenPairsInUnit(List<(int row, int col)> cells, CandidateGrid candidates, int size)
    {
        var results = new List<(int, int, (int, int), (int, int))>();
        
        // For each digit, find which cells contain it
        var digitPositions = new Dictionary<int, List<(int row, int col)>>();
        
        for (int digit = 1; digit <= size; digit++)
        {
            digitPositions[digit] = cells
                .Where(c => candidates.HasCandidate(c.row, c.col, digit))
                .ToList();
        }
        
        // Find pairs of digits that appear in exactly 2 cells
        for (int d1 = 1; d1 <= size; d1++)
        {
            if (digitPositions[d1].Count != 2) continue;
            
            for (int d2 = d1 + 1; d2 <= size; d2++)
            {
                if (digitPositions[d2].Count != 2) continue;
                
                // Check if same two cells
                var pos1 = digitPositions[d1];
                var pos2 = digitPositions[d2];
                
                if (pos1[0] == pos2[0] && pos1[1] == pos2[1])
                {
                    // These two digits appear in exactly the same two cells
                    var cell1 = pos1[0];
                    var cell2 = pos1[1];
                    
                    // Check if cells have other candidates (otherwise it's just a naked pair)
                    var cands1 = candidates.GetCandidateCount(cell1.row, cell1.col);
                    var cands2 = candidates.GetCandidateCount(cell2.row, cell2.col);
                    
                    if (cands1 > 2 || cands2 > 2)
                    {
                        results.Add((d1, d2, cell1, cell2));
                    }
                }
            }
        }
        
        return results;
    }
    
    #endregion
    
    #region X-Wing
    
    /// <summary>
    /// Detects X-Wing patterns - a digit in exactly 2 cells in 2 rows, sharing 2 columns.
    /// </summary>
    public static List<TechniqueInstance> DetectXWing(Board board, CandidateGrid candidates)
    {
        var results = new List<TechniqueInstance>();
        var size = board.Size;
        
        // Check row-based X-Wings
        for (int digit = 1; digit <= size; digit++)
        {
            // Find rows where this digit appears in exactly 2 columns
            var rowsWithTwo = new List<(int row, int col1, int col2)>();
            
            for (int row = 0; row < size; row++)
            {
                var cols = new List<int>();
                for (int col = 0; col < size; col++)
                {
                    if (candidates.HasCandidate(row, col, digit))
                    {
                        cols.Add(col);
                    }
                }
                
                if (cols.Count == 2)
                {
                    rowsWithTwo.Add((row, cols[0], cols[1]));
                }
            }
            
            // Check pairs of rows for X-Wing
            for (int i = 0; i < rowsWithTwo.Count; i++)
            {
                for (int j = i + 1; j < rowsWithTwo.Count; j++)
                {
                    var r1 = rowsWithTwo[i];
                    var r2 = rowsWithTwo[j];
                    
                    // Same columns?
                    if (r1.col1 == r2.col1 && r1.col2 == r2.col2)
                    {
                        // Check if this eliminates candidates in other rows
                        bool useful = false;
                        for (int row = 0; row < size; row++)
                        {
                            if (row == r1.row || row == r2.row) continue;
                            
                            if (candidates.HasCandidate(row, r1.col1, digit) ||
                                candidates.HasCandidate(row, r1.col2, digit))
                            {
                                useful = true;
                                break;
                            }
                        }
                        
                        if (useful)
                        {
                            results.Add(new TechniqueInstance(
                                SolvingTechnique.XWing,
                                r1.row, r1.col1,
                                $"X-Wing on {digit} in rows {r1.row + 1},{r2.row + 1} and columns {r1.col1 + 1},{r1.col2 + 1}"));
                        }
                    }
                }
            }
        }
        
        // Check column-based X-Wings
        for (int digit = 1; digit <= size; digit++)
        {
            var colsWithTwo = new List<(int col, int row1, int row2)>();
            
            for (int col = 0; col < size; col++)
            {
                var rows = new List<int>();
                for (int row = 0; row < size; row++)
                {
                    if (candidates.HasCandidate(row, col, digit))
                    {
                        rows.Add(row);
                    }
                }
                
                if (rows.Count == 2)
                {
                    colsWithTwo.Add((col, rows[0], rows[1]));
                }
            }
            
            for (int i = 0; i < colsWithTwo.Count; i++)
            {
                for (int j = i + 1; j < colsWithTwo.Count; j++)
                {
                    var c1 = colsWithTwo[i];
                    var c2 = colsWithTwo[j];
                    
                    if (c1.row1 == c2.row1 && c1.row2 == c2.row2)
                    {
                        bool useful = false;
                        for (int col = 0; col < size; col++)
                        {
                            if (col == c1.col || col == c2.col) continue;
                            
                            if (candidates.HasCandidate(c1.row1, col, digit) ||
                                candidates.HasCandidate(c1.row2, col, digit))
                            {
                                useful = true;
                                break;
                            }
                        }
                        
                        if (useful)
                        {
                            results.Add(new TechniqueInstance(
                                SolvingTechnique.XWing,
                                c1.row1, c1.col,
                                $"X-Wing on {digit} in columns {c1.col + 1},{c2.col + 1} and rows {c1.row1 + 1},{c1.row2 + 1}"));
                        }
                    }
                }
            }
        }
        
        return results;
    }
    
    #endregion
    
    #region Swordfish
    
    /// <summary>
    /// Detects Swordfish patterns - extension of X-Wing to 3 rows/columns.
    /// </summary>
    public static List<TechniqueInstance> DetectSwordfish(Board board, CandidateGrid candidates)
    {
        var results = new List<TechniqueInstance>();
        var size = board.Size;
        
        // Check row-based Swordfish
        for (int digit = 1; digit <= size; digit++)
        {
            // Find rows where digit appears in 2-3 columns
            var eligibleRows = new List<(int row, HashSet<int> cols)>();
            
            for (int row = 0; row < size; row++)
            {
                var cols = new HashSet<int>();
                for (int col = 0; col < size; col++)
                {
                    if (candidates.HasCandidate(row, col, digit))
                    {
                        cols.Add(col);
                    }
                }
                
                if (cols.Count >= 2 && cols.Count <= 3)
                {
                    eligibleRows.Add((row, cols));
                }
            }
            
            // Check triples of rows
            for (int i = 0; i < eligibleRows.Count; i++)
            {
                for (int j = i + 1; j < eligibleRows.Count; j++)
                {
                    for (int k = j + 1; k < eligibleRows.Count; k++)
                    {
                        var r1 = eligibleRows[i];
                        var r2 = eligibleRows[j];
                        var r3 = eligibleRows[k];
                        
                        // Union of columns must be exactly 3
                        var allCols = new HashSet<int>(r1.cols);
                        allCols.UnionWith(r2.cols);
                        allCols.UnionWith(r3.cols);
                        
                        if (allCols.Count == 3)
                        {
                            // Check if this eliminates candidates
                            bool useful = false;
                            var colsList = allCols.ToList();
                            
                            for (int row = 0; row < size; row++)
                            {
                                if (row == r1.row || row == r2.row || row == r3.row) continue;
                                
                                foreach (var col in colsList)
                                {
                                    if (candidates.HasCandidate(row, col, digit))
                                    {
                                        useful = true;
                                        break;
                                    }
                                }
                                if (useful) break;
                            }
                            
                            if (useful)
                            {
                                results.Add(new TechniqueInstance(
                                    SolvingTechnique.Swordfish,
                                    r1.row, colsList[0],
                                    $"Swordfish on {digit} in rows {r1.row + 1},{r2.row + 1},{r3.row + 1} and columns {string.Join(",", colsList.Select(c => c + 1))}"));
                            }
                        }
                    }
                }
            }
        }
        
        // Check column-based Swordfish
        for (int digit = 1; digit <= size; digit++)
        {
            var eligibleCols = new List<(int col, HashSet<int> rows)>();
            
            for (int col = 0; col < size; col++)
            {
                var rows = new HashSet<int>();
                for (int row = 0; row < size; row++)
                {
                    if (candidates.HasCandidate(row, col, digit))
                    {
                        rows.Add(row);
                    }
                }
                
                if (rows.Count >= 2 && rows.Count <= 3)
                {
                    eligibleCols.Add((col, rows));
                }
            }
            
            for (int i = 0; i < eligibleCols.Count; i++)
            {
                for (int j = i + 1; j < eligibleCols.Count; j++)
                {
                    for (int k = j + 1; k < eligibleCols.Count; k++)
                    {
                        var c1 = eligibleCols[i];
                        var c2 = eligibleCols[j];
                        var c3 = eligibleCols[k];
                        
                        var allRows = new HashSet<int>(c1.rows);
                        allRows.UnionWith(c2.rows);
                        allRows.UnionWith(c3.rows);
                        
                        if (allRows.Count == 3)
                        {
                            bool useful = false;
                            var rowsList = allRows.ToList();
                            
                            for (int col = 0; col < size; col++)
                            {
                                if (col == c1.col || col == c2.col || col == c3.col) continue;
                                
                                foreach (var row in rowsList)
                                {
                                    if (candidates.HasCandidate(row, col, digit))
                                    {
                                        useful = true;
                                        break;
                                    }
                                }
                                if (useful) break;
                            }
                            
                            if (useful)
                            {
                                results.Add(new TechniqueInstance(
                                    SolvingTechnique.Swordfish,
                                    rowsList[0], c1.col,
                                    $"Swordfish on {digit} in columns {c1.col + 1},{c2.col + 1},{c3.col + 1} and rows {string.Join(",", rowsList.Select(r => r + 1))}"));
                            }
                        }
                    }
                }
            }
        }
        
        return results;
    }
    
    #endregion
    
    #region XY-Wing
    
    /// <summary>
    /// Detects XY-Wing patterns. Pivot has {A,B}, wing1 has {A,C}, wing2 has {B,C}.
    /// Candidate C is eliminated from cells seeing both wings.
    /// </summary>
    public static List<TechniqueInstance> DetectXYWing(Board board, CandidateGrid candidates)
    {
        var results = new List<TechniqueInstance>();
        var size = board.Size;
        
        // Find all bivalue cells (exactly 2 candidates)
        var bivalueCells = new List<(int row, int col, uint cands)>();
        
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (candidates.GetCandidateCount(row, col) == 2)
                {
                    bivalueCells.Add((row, col, candidates[row, col]));
                }
            }
        }
        
        // For each potential pivot
        foreach (var pivot in bivalueCells)
        {
            var pivotCands = GetDigitsFromMask(pivot.cands);
            var A = pivotCands[0];
            var B = pivotCands[1];
            
            // Find cells that can see the pivot
            var visibleCells = bivalueCells
                .Where(c => c != pivot && CellsCanSeeEachOther(board, pivot.row, pivot.col, c.row, c.col))
                .ToList();
            
            // Find wing1 candidates (cells with {A,C} where C != B)
            var wing1Candidates = visibleCells
                .Where(c => {
                    var cands = GetDigitsFromMask(c.cands);
                    return cands.Contains(A) && !cands.Contains(B);
                })
                .ToList();
            
            // Find wing2 candidates (cells with {B,C} where C != A)
            var wing2Candidates = visibleCells
                .Where(c => {
                    var cands = GetDigitsFromMask(c.cands);
                    return cands.Contains(B) && !cands.Contains(A);
                })
                .ToList();
            
            // Check all combinations
            foreach (var wing1 in wing1Candidates)
            {
                var wing1Cands = GetDigitsFromMask(wing1.cands);
                var C1 = wing1Cands.First(d => d != A);
                
                foreach (var wing2 in wing2Candidates)
                {
                    var wing2Cands = GetDigitsFromMask(wing2.cands);
                    var C2 = wing2Cands.First(d => d != B);
                    
                    // Must have same C value
                    if (C1 != C2) continue;
                    var C = C1;
                    
                    // Wings must not see each other (otherwise simpler technique applies)
                    // Actually, they can see each other, but let's check for eliminations
                    
                    // Find cells that see both wings and have C as candidate
                    bool useful = false;
                    for (int row = 0; row < size; row++)
                    {
                        for (int col = 0; col < size; col++)
                        {
                            if ((row == pivot.row && col == pivot.col) ||
                                (row == wing1.row && col == wing1.col) ||
                                (row == wing2.row && col == wing2.col))
                                continue;
                            
                            if (candidates.HasCandidate(row, col, C) &&
                                CellsCanSeeEachOther(board, row, col, wing1.row, wing1.col) &&
                                CellsCanSeeEachOther(board, row, col, wing2.row, wing2.col))
                            {
                                useful = true;
                                break;
                            }
                        }
                        if (useful) break;
                    }
                    
                    if (useful)
                    {
                        results.Add(new TechniqueInstance(
                            SolvingTechnique.XYWing,
                            pivot.row, pivot.col,
                            $"XY-Wing: pivot R{pivot.row + 1}C{pivot.col + 1} ({A},{B}), " +
                            $"wings R{wing1.row + 1}C{wing1.col + 1} ({A},{C}) and R{wing2.row + 1}C{wing2.col + 1} ({B},{C}), eliminates {C}"));
                    }
                }
            }
        }
        
        return results;
    }
    
    #endregion
    
    #region XYZ-Wing
    
    /// <summary>
    /// Detects XYZ-Wing patterns. Pivot has {A,B,C}, wing1 has {A,C}, wing2 has {B,C}.
    /// Candidate C is eliminated from cells seeing all three.
    /// </summary>
    public static List<TechniqueInstance> DetectXYZWing(Board board, CandidateGrid candidates)
    {
        var results = new List<TechniqueInstance>();
        var size = board.Size;
        
        // Find trivalue cells (exactly 3 candidates) for potential pivots
        var trivalueCells = new List<(int row, int col, uint cands)>();
        var bivalueCells = new List<(int row, int col, uint cands)>();
        
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var count = candidates.GetCandidateCount(row, col);
                if (count == 3)
                {
                    trivalueCells.Add((row, col, candidates[row, col]));
                }
                else if (count == 2)
                {
                    bivalueCells.Add((row, col, candidates[row, col]));
                }
            }
        }
        
        // For each potential pivot (trivalue cell)
        foreach (var pivot in trivalueCells)
        {
            var pivotCands = GetDigitsFromMask(pivot.cands);
            var A = pivotCands[0];
            var B = pivotCands[1];
            var C = pivotCands[2];
            
            // Find bivalue cells visible to pivot
            var visibleBivalue = bivalueCells
                .Where(c => CellsCanSeeEachOther(board, pivot.row, pivot.col, c.row, c.col))
                .ToList();
            
            // Check all permutations of A, B, C for wing patterns
            var candidateOrders = new[] {
                (A, B, C), (A, C, B), (B, A, C), (B, C, A), (C, A, B), (C, B, A)
            };
            
            foreach (var (d1, d2, d3) in candidateOrders)
            {
                // Look for wing1 with {d1, d3} and wing2 with {d2, d3}
                var targetWing1 = (1u << (d1 - 1)) | (1u << (d3 - 1));
                var targetWing2 = (1u << (d2 - 1)) | (1u << (d3 - 1));
                
                var wing1Candidates = visibleBivalue.Where(c => c.cands == targetWing1).ToList();
                var wing2Candidates = visibleBivalue.Where(c => c.cands == targetWing2).ToList();
                
                foreach (var wing1 in wing1Candidates)
                {
                    foreach (var wing2 in wing2Candidates)
                    {
                        if (wing1 == wing2) continue;
                        
                        // Find cells seeing all three and having d3 as candidate
                        bool useful = false;
                        for (int row = 0; row < size; row++)
                        {
                            for (int col = 0; col < size; col++)
                            {
                                if ((row == pivot.row && col == pivot.col) ||
                                    (row == wing1.row && col == wing1.col) ||
                                    (row == wing2.row && col == wing2.col))
                                    continue;
                                
                                if (candidates.HasCandidate(row, col, d3) &&
                                    CellsCanSeeEachOther(board, row, col, pivot.row, pivot.col) &&
                                    CellsCanSeeEachOther(board, row, col, wing1.row, wing1.col) &&
                                    CellsCanSeeEachOther(board, row, col, wing2.row, wing2.col))
                                {
                                    useful = true;
                                    break;
                                }
                            }
                            if (useful) break;
                        }
                        
                        if (useful)
                        {
                            results.Add(new TechniqueInstance(
                                SolvingTechnique.XYZWing,
                                pivot.row, pivot.col,
                                $"XYZ-Wing: pivot R{pivot.row + 1}C{pivot.col + 1} ({d1},{d2},{d3}), " +
                                $"wings R{wing1.row + 1}C{wing1.col + 1} ({d1},{d3}) and R{wing2.row + 1}C{wing2.col + 1} ({d2},{d3}), eliminates {d3}"));
                        }
                    }
                }
            }
        }
        
        return results;
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Checks if two cells can see each other (same row, column, or box).
    /// </summary>
    public static bool CellsCanSeeEachOther(Board board, int row1, int col1, int row2, int col2)
    {
        if (row1 == row2) return true;
        if (col1 == col2) return true;
        if (board.GetBoxIndex(row1, col1) == board.GetBoxIndex(row2, col2)) return true;
        return false;
    }
    
    /// <summary>
    /// Converts a candidate bitmask to a list of digits.
    /// </summary>
    private static List<int> GetDigitsFromMask(uint mask)
    {
        var result = new List<int>();
        for (int d = 1; d <= 9; d++)
        {
            if ((mask & (1u << (d - 1))) != 0)
            {
                result.Add(d);
            }
        }
        return result;
    }
    
    #endregion
}

