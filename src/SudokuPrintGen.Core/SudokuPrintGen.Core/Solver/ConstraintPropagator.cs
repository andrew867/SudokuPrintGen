using System.Numerics;
using SudokuPrintGen.Core.Puzzle;

namespace SudokuPrintGen.Core.Solver;

/// <summary>
/// Vectorized constraint propagation using SIMD intrinsics.
/// </summary>
public static class ConstraintPropagator
{
    /// <summary>
    /// Propagates constraints using bit vectors for candidate tracking.
    /// Returns true if any propagation occurred.
    /// </summary>
    public static bool PropagateConstraints(Board board, Span<uint> rowCandidates, Span<uint> colCandidates, Span<uint> boxCandidates)
    {
        bool changed = false;
        var size = board.Size;
        
        // Initialize candidate sets using SIMD when available
        SimdConstraintPropagator.InitializeCandidatesVectorized(rowCandidates, size);
        SimdConstraintPropagator.InitializeCandidatesVectorized(colCandidates, size);
        SimdConstraintPropagator.InitializeCandidatesVectorized(boxCandidates, size);
        
        // Process given clues
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var value = board[row, col];
                if (value != 0)
                {
                    var mask = ~(1u << (value - 1)); // Clear the bit for this value
                    rowCandidates[row] &= mask;
                    colCandidates[col] &= mask;
                    var boxIndex = board.GetBoxIndex(row, col);
                    boxCandidates[boxIndex] &= mask;
                }
            }
        }
        
        // Propagate: find cells with only one candidate
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (board[row, col] != 0)
                    continue;
                
                var boxIndex = board.GetBoxIndex(row, col);
                var candidates = rowCandidates[row] & colCandidates[col] & boxCandidates[boxIndex];
                
                // Count candidates using bit operations
                var candidateCount = BitOperations.PopCount(candidates);
                
                if (candidateCount == 1)
                {
                    // Find which bit is set
                    var value = BitOperations.TrailingZeroCount(candidates) + 1;
                    board[row, col] = value;
                    
                    // Update candidate sets
                    var mask = ~(1u << (value - 1));
                    rowCandidates[row] &= mask;
                    colCandidates[col] &= mask;
                    boxCandidates[boxIndex] &= mask;
                    
                    changed = true;
                }
            }
        }
        
        return changed;
    }
    
    /// <summary>
    /// Gets candidates for a cell using bitwise operations.
    /// </summary>
    public static List<int> GetCandidates(Board board, int row, int col, Span<uint> rowCandidates, Span<uint> colCandidates, Span<uint> boxCandidates)
    {
        var boxIndex = board.GetBoxIndex(row, col);
        var candidates = rowCandidates[row] & colCandidates[col] & boxCandidates[boxIndex];
        
        var result = new List<int>();
        var temp = candidates;
        int bit = 0;
        while (temp != 0)
        {
            if ((temp & 1) != 0)
            {
                result.Add(bit + 1);
            }
            temp >>= 1;
            bit++;
        }
        
        return result;
    }
}

