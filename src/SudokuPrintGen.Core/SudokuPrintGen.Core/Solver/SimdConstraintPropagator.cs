using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using SudokuPrintGen.Core.Puzzle;

namespace SudokuPrintGen.Core.Solver;

/// <summary>
/// Advanced SIMD-optimized constraint propagation using hardware intrinsics.
/// </summary>
public static class SimdConstraintPropagator
{
    /// <summary>
    /// Vectorized initialization of candidate sets using SIMD.
    /// </summary>
    public static void InitializeCandidatesVectorized(Span<uint> candidates, int size)
    {
        const uint allCandidates = 0x1FF; // Bits 0-8 set
        
        // Use Vector<uint> for SIMD initialization when available
        if (Vector.IsHardwareAccelerated && size >= Vector<uint>.Count)
        {
            var vector = new Vector<uint>(allCandidates);
            int i = 0;
            for (; i <= size - Vector<uint>.Count; i += Vector<uint>.Count)
            {
                vector.CopyTo(candidates.Slice(i));
            }
            
            // Handle remaining elements
            for (; i < size; i++)
            {
                candidates[i] = allCandidates;
            }
        }
        else
        {
            // Fallback to scalar
            for (int i = 0; i < size; i++)
            {
                candidates[i] = allCandidates;
            }
        }
    }
    
    /// <summary>
    /// Vectorized candidate elimination using AVX2 when available.
    /// </summary>
    public static void EliminateCandidatesVectorized(Span<uint> candidates, uint mask, int count)
    {
        if (Avx2.IsSupported && count >= 8)
        {
            // Process 8 uint32s at a time with AVX2
            var maskVector = Vector256.Create(mask);
            int i = 0;
            unsafe
            {
                fixed (uint* ptr = candidates)
                {
                    for (; i <= count - 8; i += 8)
                    {
                        var vec = Avx2.LoadVector256(ptr + i);
                        var result = Avx2.And(vec, maskVector);
                        Avx2.Store(ptr + i, result);
                    }
                }
            }
            
            // Handle remaining elements
            for (; i < count; i++)
            {
                candidates[i] &= mask;
            }
        }
        else if (Sse2.IsSupported && count >= 4)
        {
            // Process 4 uint32s at a time with SSE2
            var maskVector = Vector128.Create(mask);
            int i = 0;
            unsafe
            {
                fixed (uint* ptr = candidates)
                {
                    for (; i <= count - 4; i += 4)
                    {
                        var vec = Sse2.LoadVector128(ptr + i);
                        var result = Sse2.And(vec, maskVector);
                        Sse2.Store(ptr + i, result);
                    }
                }
            }
            
            // Handle remaining elements
            for (; i < count; i++)
            {
                candidates[i] &= mask;
            }
        }
        else
        {
            // Fallback to scalar
            for (int i = 0; i < count; i++)
            {
                candidates[i] &= mask;
            }
        }
    }
    
    /// <summary>
    /// Vectorized popcount for multiple candidate sets.
    /// </summary>
    public static void PopCountVectorized(ReadOnlySpan<uint> candidates, Span<int> counts)
    {
        if (Popcnt.IsSupported && candidates.Length >= 4)
        {
            int i = 0;
            for (; i <= candidates.Length - 4; i += 4)
            {
                counts[i] = (int)Popcnt.PopCount(candidates[i]);
                counts[i + 1] = (int)Popcnt.PopCount(candidates[i + 1]);
                counts[i + 2] = (int)Popcnt.PopCount(candidates[i + 2]);
                counts[i + 3] = (int)Popcnt.PopCount(candidates[i + 3]);
            }
            
            // Handle remaining
            for (; i < candidates.Length; i++)
            {
                counts[i] = (int)Popcnt.PopCount(candidates[i]);
            }
        }
        else
        {
            // Fallback to BitOperations
            for (int i = 0; i < candidates.Length; i++)
            {
                counts[i] = BitOperations.PopCount(candidates[i]);
            }
        }
    }
}

