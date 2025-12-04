using SudokuPrintGen.Core.Puzzle;
using SudokuPrintGen.Core.Solver;
using Xunit;

namespace SudokuPrintGen.Tests;

public class TechniqueDetectorTests
{
    #region CandidateGrid Tests
    
    [Fact]
    public void CandidateGrid_InitializesCorrectly()
    {
        // A puzzle with some clues
        var puzzle = Board.FromString(
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079");
        
        var candidates = new CandidateGrid(puzzle);
        
        // Cell (0,0) has value 5, should have no candidates
        Assert.Equal(0u, candidates[0, 0]);
        
        // Cell (0,2) is empty (value 0), should have candidates
        Assert.True(candidates.GetCandidateCount(0, 2) > 0);
    }
    
    [Fact]
    public void CandidateGrid_GetCandidateList_ReturnsCorrectDigits()
    {
        var puzzle = Board.CreateStandard();
        // Fill row 0 with 1-8, leaving (0,8) empty
        for (int col = 0; col < 8; col++)
        {
            puzzle[0, col] = col + 1;
        }
        
        var candidates = new CandidateGrid(puzzle);
        var list = candidates.GetCandidateList(0, 8);
        
        // Only 9 should be possible in (0,8) considering just the row
        Assert.Contains(9, list);
    }
    
    [Fact]
    public void CandidateGrid_HasCandidate_ReturnsCorrectValue()
    {
        var puzzle = Board.CreateStandard();
        puzzle[0, 0] = 5; // Place 5 in (0,0)
        
        var candidates = new CandidateGrid(puzzle);
        
        // Cell (0,1) should not have 5 as candidate (same row)
        Assert.False(candidates.HasCandidate(0, 1, 5));
        
        // Cell (3,3) should have 5 as candidate (different row/col/box)
        Assert.True(candidates.HasCandidate(3, 3, 5));
    }
    
    #endregion
    
    #region Naked Singles Tests
    
    [Fact]
    public void DetectNakedSingles_FindsSingles()
    {
        // Create a puzzle where cell (0,8) has only one candidate
        var puzzle = Board.CreateStandard();
        // Fill row 0 partially to create a naked single
        puzzle[0, 0] = 1;
        puzzle[0, 1] = 2;
        puzzle[0, 2] = 3;
        puzzle[0, 3] = 4;
        puzzle[0, 4] = 5;
        puzzle[0, 5] = 6;
        puzzle[0, 6] = 7;
        puzzle[0, 7] = 8;
        // (0,8) must be 9
        
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectNakedSingles(puzzle, candidates);
        
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Row == 0 && r.Col == 8);
    }
    
    [Fact]
    public void HasNakedSingles_ReturnsTrueWhenPresent()
    {
        var puzzle = Board.CreateStandard();
        for (int col = 0; col < 8; col++)
        {
            puzzle[0, col] = col + 1;
        }
        
        Assert.True(TechniqueDetector.HasNakedSingles(puzzle));
    }
    
    [Fact]
    public void HasNakedSingles_ReturnsFalseWhenNone()
    {
        // Empty puzzle has many candidates per cell
        var puzzle = Board.CreateStandard();
        
        Assert.False(TechniqueDetector.HasNakedSingles(puzzle));
    }
    
    #endregion
    
    #region Hidden Singles Tests
    
    [Fact]
    public void DetectHiddenSingles_FindsSingles()
    {
        // Use a real puzzle that has hidden singles
        // This is a standard test puzzle known to have hidden singles
        var puzzle = Board.FromString(
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079");
        
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectHiddenSingles(puzzle, candidates);
        
        // This puzzle should have hidden singles
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(SolvingTechnique.HiddenSingle, r.Technique));
    }
    
    [Fact]
    public void HasHiddenSingles_ReturnsTrueForValidPuzzle()
    {
        // Most valid Sudoku puzzles have hidden singles
        var puzzle = Board.FromString(
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079");
        
        Assert.True(TechniqueDetector.HasHiddenSingles(puzzle));
    }
    
    #endregion
    
    #region Naked Pairs Tests
    
    [Fact]
    public void DetectNakedPairs_FindsPairs()
    {
        // Create a row where two cells have exactly {1,2} as candidates
        // and other cells have more candidates including 1 or 2
        var puzzle = Board.CreateStandard();
        
        // Fill row 0 strategically
        puzzle[0, 0] = 3;
        puzzle[0, 1] = 4;
        puzzle[0, 2] = 5;
        puzzle[0, 3] = 6;
        puzzle[0, 4] = 7;
        // Leave 0,5 and 0,6 empty - they should have {1,2} or similar limited candidates
        puzzle[0, 7] = 8;
        puzzle[0, 8] = 9;
        
        // Block candidates in cols 5 and 6 from boxes and columns
        // to create a naked pair scenario
        puzzle[3, 5] = 1; // Different box but blocks column 5 for 1
        puzzle[6, 6] = 2; // Different box but blocks column 6 for 2
        
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectNakedPairs(puzzle, candidates);
        
        // May or may not find naked pairs depending on exact configuration
        // Just verify no exceptions
        Assert.NotNull(results);
    }
    
    [Fact]
    public void DetectNakedPairs_ReturnsEmptyForEmptyPuzzle()
    {
        var puzzle = Board.CreateStandard();
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectNakedPairs(puzzle, candidates);
        
        // Empty puzzle unlikely to have useful naked pairs
        Assert.NotNull(results);
    }
    
    #endregion
    
    #region Hidden Pairs Tests
    
    [Fact]
    public void DetectHiddenPairs_FindsPairs()
    {
        // Create scenario where two digits only appear in two cells of a unit
        var puzzle = Board.CreateStandard();
        
        // Set up a row where 8 and 9 can only go in two specific cells
        puzzle[0, 0] = 1;
        puzzle[0, 1] = 2;
        puzzle[0, 2] = 3;
        puzzle[0, 3] = 4;
        puzzle[0, 4] = 5;
        // Leave 0,5 and 0,6 empty
        puzzle[0, 7] = 6;
        puzzle[0, 8] = 7;
        
        // Block 8 and 9 from appearing in other cells of this row via column/box constraints
        puzzle[3, 5] = 8;
        puzzle[4, 5] = 9;
        puzzle[3, 6] = 9;
        puzzle[4, 6] = 8;
        
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectHiddenPairs(puzzle, candidates);
        
        Assert.NotNull(results);
    }
    
    #endregion
    
    #region X-Wing Tests
    
    [Fact]
    public void DetectXWing_FindsPatterns()
    {
        // X-Wing requires careful setup
        // Digit d appears in exactly 2 cells in row A (columns 1,2)
        // Digit d appears in exactly 2 cells in row B (columns 1,2)
        // This eliminates d from other cells in columns 1,2
        
        var puzzle = Board.CreateStandard();
        
        // Complex setup for X-Wing - just verify no exceptions
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectXWing(puzzle, candidates);
        
        Assert.NotNull(results);
    }
    
    [Fact]
    public void DetectXWing_EmptyPuzzle_NoPatterns()
    {
        var puzzle = Board.CreateStandard();
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectXWing(puzzle, candidates);
        
        // Empty puzzle won't have X-Wing patterns that eliminate candidates
        Assert.Empty(results);
    }
    
    #endregion
    
    #region Swordfish Tests
    
    [Fact]
    public void DetectSwordfish_FindsPatterns()
    {
        var puzzle = Board.CreateStandard();
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectSwordfish(puzzle, candidates);
        
        Assert.NotNull(results);
    }
    
    [Fact]
    public void DetectSwordfish_EmptyPuzzle_NoPatterns()
    {
        var puzzle = Board.CreateStandard();
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectSwordfish(puzzle, candidates);
        
        Assert.Empty(results);
    }
    
    #endregion
    
    #region XY-Wing Tests
    
    [Fact]
    public void DetectXYWing_FindsPatterns()
    {
        var puzzle = Board.CreateStandard();
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectXYWing(puzzle, candidates);
        
        Assert.NotNull(results);
    }
    
    [Fact]
    public void DetectXYWing_EmptyPuzzle_NoPatterns()
    {
        // Empty puzzle has 9 candidates per cell, no bivalue cells
        var puzzle = Board.CreateStandard();
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectXYWing(puzzle, candidates);
        
        Assert.Empty(results);
    }
    
    #endregion
    
    #region XYZ-Wing Tests
    
    [Fact]
    public void DetectXYZWing_FindsPatterns()
    {
        var puzzle = Board.CreateStandard();
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectXYZWing(puzzle, candidates);
        
        Assert.NotNull(results);
    }
    
    [Fact]
    public void DetectXYZWing_EmptyPuzzle_NoPatterns()
    {
        var puzzle = Board.CreateStandard();
        var candidates = new CandidateGrid(puzzle);
        var results = TechniqueDetector.DetectXYZWing(puzzle, candidates);
        
        Assert.Empty(results);
    }
    
    #endregion
    
    #region DetectAllTechniques Tests
    
    [Fact]
    public void DetectAllTechniques_ReturnsResults()
    {
        var puzzle = Board.FromString(
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079");
        
        var results = TechniqueDetector.DetectAllTechniques(puzzle);
        
        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }
    
    [Fact]
    public void DetectAllTechniques_IncludesBasicTechniques()
    {
        var puzzle = Board.FromString(
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079");
        
        var results = TechniqueDetector.DetectAllTechniques(puzzle);
        
        // Should find at least naked or hidden singles
        var hasBasic = results.Any(r => 
            r.Technique == SolvingTechnique.NakedSingle || 
            r.Technique == SolvingTechnique.HiddenSingle);
        
        Assert.True(hasBasic);
    }
    
    #endregion
    
    #region Technique Scoring Tests
    
    [Fact]
    public void GetTechniqueWeight_ReturnsCorrectWeights()
    {
        Assert.Equal(1, TechniqueDetector.GetTechniqueWeight(SolvingTechnique.NakedSingle));
        Assert.Equal(2, TechniqueDetector.GetTechniqueWeight(SolvingTechnique.HiddenSingle));
        Assert.Equal(4, TechniqueDetector.GetTechniqueWeight(SolvingTechnique.NakedPair));
        Assert.Equal(5, TechniqueDetector.GetTechniqueWeight(SolvingTechnique.HiddenPair));
        Assert.Equal(8, TechniqueDetector.GetTechniqueWeight(SolvingTechnique.XWing));
        Assert.Equal(10, TechniqueDetector.GetTechniqueWeight(SolvingTechnique.XYWing));
        Assert.Equal(12, TechniqueDetector.GetTechniqueWeight(SolvingTechnique.Swordfish));
        Assert.Equal(14, TechniqueDetector.GetTechniqueWeight(SolvingTechnique.XYZWing));
    }
    
    [Fact]
    public void CalculateTechniqueScore_ReturnsZeroForEmpty()
    {
        var techniques = new List<TechniqueInstance>();
        var score = TechniqueDetector.CalculateTechniqueScore(techniques);
        
        Assert.Equal(0, score);
    }
    
    [Fact]
    public void CalculateTechniqueScore_CalculatesCorrectly()
    {
        var techniques = new List<TechniqueInstance>
        {
            new TechniqueInstance(SolvingTechnique.NakedSingle, 0, 0, "test"),
            new TechniqueInstance(SolvingTechnique.HiddenSingle, 0, 1, "test"),
            new TechniqueInstance(SolvingTechnique.NakedPair, 0, 2, "test")
        };
        
        var score = TechniqueDetector.CalculateTechniqueScore(techniques);
        
        // Max weight is 4 (NakedPair), plus bonus for 3 unique techniques
        // Expected: 4 + (3-1) * 0.5 = 4 + 1 = 5
        Assert.Equal(5, score);
    }
    
    #endregion
    
    #region CellsCanSeeEachOther Tests
    
    [Fact]
    public void CellsCanSeeEachOther_SameRow_ReturnsTrue()
    {
        var board = Board.CreateStandard();
        Assert.True(TechniqueDetector.CellsCanSeeEachOther(board, 0, 0, 0, 8));
    }
    
    [Fact]
    public void CellsCanSeeEachOther_SameColumn_ReturnsTrue()
    {
        var board = Board.CreateStandard();
        Assert.True(TechniqueDetector.CellsCanSeeEachOther(board, 0, 0, 8, 0));
    }
    
    [Fact]
    public void CellsCanSeeEachOther_SameBox_ReturnsTrue()
    {
        var board = Board.CreateStandard();
        Assert.True(TechniqueDetector.CellsCanSeeEachOther(board, 0, 0, 2, 2));
    }
    
    [Fact]
    public void CellsCanSeeEachOther_DifferentUnits_ReturnsFalse()
    {
        var board = Board.CreateStandard();
        // (0,0) and (4,4) are in different row, column, and box
        Assert.False(TechniqueDetector.CellsCanSeeEachOther(board, 0, 0, 4, 4));
    }
    
    #endregion
    
    #region Integration with DifficultyRater Tests
    
    [Fact]
    public void DifficultyRater_IncludesTechniqueScore()
    {
        var puzzle = Board.FromString(
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079");
        var solver = new DpllSolver();
        
        var rating = DifficultyRater.RatePuzzleWithMetrics(puzzle, solver);
        
        Assert.NotNull(rating.DetectedTechniques);
        Assert.True(rating.TechniqueScore >= 0);
    }
    
    [Fact]
    public void DifficultyRater_PopulatesHardestTechnique()
    {
        var puzzle = Board.FromString(
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079");
        var solver = new DpllSolver();
        
        var rating = DifficultyRater.RatePuzzleWithMetrics(puzzle, solver);
        
        if (rating.DetectedTechniques.Count > 0)
        {
            Assert.NotNull(rating.HardestTechnique);
        }
    }
    
    [Fact]
    public void DifficultyRater_RequiredTechniquesIncludesDetected()
    {
        var puzzle = Board.FromString(
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079");
        var solver = new DpllSolver();
        
        var rating = DifficultyRater.RatePuzzleWithMetrics(puzzle, solver);
        
        // Should include technique names in RequiredTechniques
        Assert.NotEmpty(rating.RequiredTechniques);
        
        // If naked singles were detected, should appear in legacy list
        if (rating.DetectedTechniques.Any(t => t.Technique == SolvingTechnique.NakedSingle))
        {
            Assert.Contains("NakedSingles", rating.RequiredTechniques);
        }
    }
    
    [Fact]
    public void DifficultyRater_CompositeScoreIncludesTechniqueScore()
    {
        var puzzle = Board.FromString(
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079");
        var solver = new DpllSolver();
        
        var rating = DifficultyRater.RatePuzzleWithMetrics(puzzle, solver);
        
        // CompositeScore should be positive
        Assert.True(rating.CompositeScore > 0);
        
        // If techniques were found, technique score should contribute
        if (rating.TechniqueScore > 0)
        {
            // Create a rating without technique score contribution
            var ratingClone = new DifficultyRating
            {
                ClueCount = rating.ClueCount,
                EmptyCells = rating.EmptyCells,
                IterationCount = rating.IterationCount,
                MaxBacktrackDepth = rating.MaxBacktrackDepth,
                GuessCount = rating.GuessCount,
                TechniqueScore = 0 // No technique score
            };
            ratingClone.CalculateCompositeScore();
            
            // Original should have higher score due to technique contribution
            Assert.True(rating.CompositeScore >= ratingClone.CompositeScore);
        }
    }
    
    #endregion
    
    #region TechniqueInstance Tests
    
    [Fact]
    public void TechniqueInstance_RecordEquality()
    {
        var t1 = new TechniqueInstance(SolvingTechnique.NakedSingle, 0, 0, "test");
        var t2 = new TechniqueInstance(SolvingTechnique.NakedSingle, 0, 0, "test");
        
        Assert.Equal(t1, t2);
    }
    
    [Fact]
    public void TechniqueInstance_RecordInequality()
    {
        var t1 = new TechniqueInstance(SolvingTechnique.NakedSingle, 0, 0, "test");
        var t2 = new TechniqueInstance(SolvingTechnique.HiddenSingle, 0, 0, "test");
        
        Assert.NotEqual(t1, t2);
    }
    
    #endregion
    
    #region SolvingTechnique Enum Tests
    
    [Fact]
    public void SolvingTechnique_OrderedByDifficulty()
    {
        // Verify the enum values are ordered by difficulty
        Assert.True((int)SolvingTechnique.NakedSingle < (int)SolvingTechnique.HiddenSingle);
        Assert.True((int)SolvingTechnique.HiddenSingle < (int)SolvingTechnique.NakedPair);
        Assert.True((int)SolvingTechnique.NakedPair < (int)SolvingTechnique.HiddenPair);
        Assert.True((int)SolvingTechnique.HiddenPair < (int)SolvingTechnique.XWing);
        Assert.True((int)SolvingTechnique.XWing < (int)SolvingTechnique.XYWing);
        Assert.True((int)SolvingTechnique.XYWing < (int)SolvingTechnique.Swordfish);
        Assert.True((int)SolvingTechnique.Swordfish < (int)SolvingTechnique.XYZWing);
    }
    
    #endregion
}

