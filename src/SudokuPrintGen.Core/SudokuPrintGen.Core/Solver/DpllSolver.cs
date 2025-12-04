using SudokuPrintGen.Core.Puzzle;

namespace SudokuPrintGen.Core.Solver;

/// <summary>
/// DPLL-based Sudoku solver with constraint propagation and metrics tracking.
/// </summary>
public class DpllSolver : ISolver
{
    private int _solutionCount;
    private int _solutionLimit;
    private Board? _firstSolution;
    private SolverMetrics _metrics = new();
    
    public Board? Solve(Board puzzle)
    {
        var result = SolveWithMetrics(puzzle);
        return result.Solution;
    }
    
    public SolverResult SolveWithMetrics(Board puzzle)
    {
        _solutionCount = 0;
        _solutionLimit = 1;
        _firstSolution = null;
        _metrics.Reset();
        
        var workingBoard = puzzle.Clone();
        SolveInternalWithMetrics(workingBoard);
        
        if (_firstSolution != null)
        {
            return SolverResult.WithSolution(_firstSolution, _metrics);
        }
        
        return SolverResult.NoSolution(_metrics);
    }
    
    public int CountSolutions(Board puzzle, int limit = 2)
    {
        var result = CountSolutionsWithMetrics(puzzle, limit);
        return result.SolutionCount;
    }
    
    public SolverResult CountSolutionsWithMetrics(Board puzzle, int limit = 2)
    {
        _solutionCount = 0;
        _solutionLimit = limit;
        _firstSolution = null;
        _metrics.Reset();
        
        var workingBoard = puzzle.Clone();
        SolveInternalWithMetrics(workingBoard);
        
        return SolverResult.ForCounting(_solutionCount, _firstSolution, _metrics);
    }
    
    public bool HasUniqueSolution(Board puzzle)
    {
        return CountSolutions(puzzle, 2) == 1;
    }
    
    public SolverResult HasUniqueSolutionWithMetrics(Board puzzle)
    {
        var result = CountSolutionsWithMetrics(puzzle, 2);
        return result;
    }
    
    private bool SolveInternalWithMetrics(Board board)
    {
        _metrics.IncrementIteration();
        _metrics.EnterLevel();
        
        try
        {
            // Unit propagation: fill cells with only one candidate
            while (PropagateConstraintsWithMetrics(board))
            {
                // Continue propagating until no more single candidates
            }
            
            // Check if solved
            if (board.IsComplete())
            {
                _solutionCount++;
                if (_firstSolution == null)
                {
                    _firstSolution = board.Clone();
                }
                return _solutionCount >= _solutionLimit;
            }
            
            // Check if invalid (empty cell with no candidates)
            if (HasEmptyCellWithNoCandidates(board))
            {
                return false;
            }
            
            // Find the cell with fewest candidates (most constrained)
            var (row, col, candidates) = FindBestCell(board);
            if (row == -1)
            {
                return false;
            }
            
            // Record that we're making a guess (branching decision)
            if (candidates.Count > 1)
            {
                _metrics.RecordGuess();
            }
            
            // Try each candidate
            foreach (var candidate in candidates)
            {
                var testBoard = board.Clone();
                testBoard[row, col] = candidate;
                
                if (SolveInternalWithMetrics(testBoard))
                {
                    // Copy solution back
                    for (int r = 0; r < board.Size; r++)
                    {
                        for (int c = 0; c < board.Size; c++)
                        {
                            board[r, c] = testBoard[r, c];
                        }
                    }
                    return true;
                }
                
                if (_solutionCount >= _solutionLimit)
                {
                    return true;
                }
            }
            
            return false;
        }
        finally
        {
            _metrics.ExitLevel();
        }
    }
    
    private bool PropagateConstraintsWithMetrics(Board board)
    {
        _metrics.RecordPropagation();
        
        // Use optimized bit-vector propagation if available
        var size = board.Size;
        Span<uint> rowCandidates = stackalloc uint[size];
        Span<uint> colCandidates = stackalloc uint[size];
        Span<uint> boxCandidates = stackalloc uint[size];
        
        return ConstraintPropagator.PropagateConstraints(board, rowCandidates, colCandidates, boxCandidates);
    }
    
    private bool HasEmptyCellWithNoCandidates(Board board)
    {
        // Use optimized candidate lookup
        var size = board.Size;
        Span<uint> rowCandidates = stackalloc uint[size];
        Span<uint> colCandidates = stackalloc uint[size];
        Span<uint> boxCandidates = stackalloc uint[size];
        
        // Initialize candidate sets
        const uint allCandidates = 0x1FF;
        for (int i = 0; i < size; i++)
        {
            rowCandidates[i] = allCandidates;
            colCandidates[i] = allCandidates;
            boxCandidates[i] = allCandidates;
        }
        
        // Process given clues
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var value = board[row, col];
                if (value != 0)
                {
                    var mask = ~(1u << (value - 1));
                    rowCandidates[row] &= mask;
                    colCandidates[col] &= mask;
                    var boxIndex = board.GetBoxIndex(row, col);
                    boxCandidates[boxIndex] &= mask;
                }
            }
        }
        
        // Check for empty cells with no candidates
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (board[row, col] == 0)
                {
                    var boxIndex = board.GetBoxIndex(row, col);
                    var candidates = rowCandidates[row] & colCandidates[col] & boxCandidates[boxIndex];
                    if (candidates == 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    
    private (int row, int col, List<int> candidates) FindBestCell(Board board)
    {
        int bestRow = -1;
        int bestCol = -1;
        List<int> bestCandidates = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        
        // Use optimized candidate lookup
        var size = board.Size;
        Span<uint> rowCandidates = stackalloc uint[size];
        Span<uint> colCandidates = stackalloc uint[size];
        Span<uint> boxCandidates = stackalloc uint[size];
        
        // Initialize candidate sets
        const uint allCandidates = 0x1FF;
        for (int i = 0; i < size; i++)
        {
            rowCandidates[i] = allCandidates;
            colCandidates[i] = allCandidates;
            boxCandidates[i] = allCandidates;
        }
        
        // Process given clues
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var value = board[row, col];
                if (value != 0)
                {
                    var mask = ~(1u << (value - 1));
                    rowCandidates[row] &= mask;
                    colCandidates[col] &= mask;
                    var boxIndex = board.GetBoxIndex(row, col);
                    boxCandidates[boxIndex] &= mask;
                }
            }
        }
        
        for (int row = 0; row < board.Size; row++)
        {
            for (int col = 0; col < board.Size; col++)
            {
                if (board[row, col] != 0)
                    continue;
                
                var candidates = ConstraintPropagator.GetCandidates(board, row, col, rowCandidates, colCandidates, boxCandidates);
                if (candidates.Count > 0 && candidates.Count < bestCandidates.Count)
                {
                    bestRow = row;
                    bestCol = col;
                    bestCandidates = candidates;
                    
                    // Early exit if we find a cell with only one candidate
                    if (candidates.Count == 1)
                    {
                        break;
                    }
                }
            }
            
            if (bestCandidates.Count == 1)
                break;
        }
        
        return (bestRow, bestCol, bestCandidates);
    }
    
    private List<int> GetCandidates(Board board, int row, int col)
    {
        var candidates = new List<int>();
        var size = board.Size;
        
        for (int value = 1; value <= size; value++)
        {
            if (IsValidPlacement(board, row, col, value))
            {
                candidates.Add(value);
            }
        }
        
        return candidates;
    }
    
    private bool IsValidPlacement(Board board, int row, int col, int value)
    {
        // Check row
        for (int c = 0; c < board.Size; c++)
        {
            if (c != col && board[row, c] == value)
                return false;
        }
        
        // Check column
        for (int r = 0; r < board.Size; r++)
        {
            if (r != row && board[r, col] == value)
                return false;
        }
        
        // Check box
        var boxIndex = board.GetBoxIndex(row, col);
        foreach (var (r, c, v) in board.GetBoxCells(boxIndex))
        {
            if (r != row && c != col && v == value)
                return false;
        }
        
        return true;
    }
}
