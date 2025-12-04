# Difficulty System Documentation

## Overview

The SudokuPrintGen difficulty system uses an iteration-based approach to measure and target puzzle difficulty. This research-backed method provides more accurate and consistent difficulty targeting compared to simple clue-count based approaches.

## How It Works

### Traditional Approach (Clue Count)

The traditional method targets a specific number of clues:
- Easy: ~40 clues (~49% filled)
- Medium: ~32 clues (~39% filled)
- Hard: ~26 clues (~32% filled)
- Expert: ~20 clues (~25% filled)
- Evil: ~17 clues (~21% filled)

**Problem**: Clue count doesn't accurately reflect solving difficulty. Two puzzles with the same number of clues can have vastly different difficulty levels depending on clue placement and constraint patterns.

### Iteration-Based Approach

The new system measures difficulty by tracking solver metrics:

1. **Iteration Count** (Primary metric, 50% weight)
   - Number of recursive solver calls
   - Higher = harder puzzle

2. **Max Backtrack Depth** (20% weight)
   - Maximum depth of the backtracking tree
   - Deeper = more complex solving path

3. **Guess Count** (20% weight)
   - Number of branching decisions made
   - More guesses = harder puzzle

4. **Clue Ratio** (10% weight)
   - Fewer clues = harder puzzle
   - Secondary factor for fine-tuning

### Difficulty Ranges

| Difficulty | Iteration Range | Score Range | Target Goal |
|------------|-----------------|-------------|-------------|
| Easy       | 1-10            | 0-8         | 5           |
| Medium     | 11-25           | 8-20        | 15          |
| Hard       | 26-80           | 20-60       | 40          |
| Expert     | 81-350          | 60-250      | 150         |
| Evil       | 351+            | 250+        | 400         |

## Iterative Refinement

When `--refine-difficulty` is enabled, the generator uses an iterative refinement process:

```
1. Generate initial puzzle with approximate clue count
2. Rate puzzle difficulty using solver metrics
3. Compare to target difficulty range
4. If too easy:
   - Remove strategic clues to increase difficulty
   - Prioritize clues in over-constrained regions
5. If too hard:
   - Add strategic clues to decrease difficulty
   - Prioritize positions that create immediate deductions
6. Repeat until puzzle matches target range or max iterations reached
```

### Strategic Clue Management

**Increasing Difficulty (Removing Clues)**:
- Remove clues from over-constrained regions first
- Prioritize clues with lower importance scores
- Maintain puzzle uniqueness
- Optionally preserve symmetry

**Decreasing Difficulty (Adding Clues)**:
- Add clues to under-constrained regions first
- Prioritize positions that create naked/hidden singles
- Balance clue distribution across regions

## Usage

### Basic Generation (Traditional Method)
```bash
sudoku-printgen generate -d Medium
```

### Iterative Refinement (Accurate Targeting)
```bash
sudoku-printgen generate -d Medium --refine-difficulty
```

### With Statistics
```bash
sudoku-printgen generate -d Hard -c 10 --refine-difficulty --show-statistics
```

### Verbose Output
```bash
sudoku-printgen generate -d Expert --refine-difficulty --verbose
```

## CLI Options

| Option | Description |
|--------|-------------|
| `--refine-difficulty` | Enable iterative refinement for accurate difficulty targeting |
| `--show-statistics` | Display generation statistics after completion |
| `--verbose` | Show detailed progress during generation |

## Statistics Report

The statistics report includes:

- **Count**: Number of puzzles generated per difficulty
- **Avg Iter**: Average solver iterations
- **Std Dev**: Standard deviation of iterations
- **Success %**: Percentage matching target difficulty
- **Avg Score**: Average composite difficulty score

Example output:
```
=== Generation Statistics Report ===

Total Puzzles Generated: 10

Difficulty    | Count | Avg Iter | Std Dev | Success % | Avg Score
--------------|-------|----------|---------|-----------|----------
Easy          |     5 |      6.2 |    1.43 |     100.0% |      5.1
Medium        |     5 |     17.4 |    3.21 |      80.0% |     14.2

=== Difficulty Targets Reference ===

Difficulty | Iteration Range | Score Range
-----------|-----------------|-------------
Easy       | 1-10            | 0-8
Medium     | 11-25           | 8-20
Hard       | 26-80           | 20-60
Expert     | 81-350          | 60-250
Evil       | 351+            | 250+
```

## API Usage

### Rating a Puzzle
```csharp
var solver = new DpllSolver();
var rating = DifficultyRater.RatePuzzleWithMetrics(puzzle, solver);

Console.WriteLine($"Iterations: {rating.IterationCount}");
Console.WriteLine($"Score: {rating.CompositeScore}");
Console.WriteLine($"Difficulty: {rating.EstimatedDifficulty}");
```

### Checking Difficulty Match
```csharp
bool matches = DifficultyTargets.IsScoreInRange(rating.CompositeScore, Difficulty.Medium);
```

### Generating with Refinement
```csharp
var generator = new PuzzleGenerator(seed: 12345);
var puzzle = generator.GenerateWithDifficultyTarget(Difficulty.Hard);

// Check if refinement succeeded
bool success = puzzle.DifficultyRating?.IsInTargetRange ?? false;
```

### Tracking Statistics
```csharp
var generator = new PuzzleGenerator();

for (int i = 0; i < 10; i++)
{
    generator.Generate(Difficulty.Medium, useIterativeRefinement: true);
}

Console.WriteLine(generator.GetStatisticsReport());
```

## Technical Details

### Composite Score Calculation

```csharp
score = (iterationCount * 0.40) +
        (techniqueScore * 2.0 * 0.20) +
        (maxBacktrackDepth * 2.0 * 0.15) +
        (guessCount * 3.0 * 0.15) +
        ((1.0 - clueRatio) * 20.0 * 0.10)
```

### Technique Detection

The system detects and weighs solving techniques using `TechniqueDetector`:

| Technique | Weight | Description |
|-----------|--------|-------------|
| Naked Single | 1 | Cell with only one candidate |
| Hidden Single | 2 | Digit can only go in one cell in a unit |
| Naked Pair | 4 | Two cells with identical 2-candidate sets |
| Hidden Pair | 5 | Two digits appearing in exactly 2 cells |
| X-Wing | 8 | Digit in exactly 2 cells in 2 rows sharing 2 columns |
| XY-Wing | 10 | Three-cell chain pattern |
| Swordfish | 12 | Extension of X-Wing to 3 rows/columns |
| XYZ-Wing | 14 | Three-cell chain with pivot having 3 candidates |

The technique score is calculated as:
```csharp
techniqueScore = maxTechniqueWeight + (uniqueTechniqueCount - 1) * 0.5
```

### Clue Distribution Analysis

The system analyzes clue distribution to identify:
- **Over-constrained regions**: Rows/columns/boxes with more clues than average
- **Under-constrained regions**: Rows/columns/boxes with fewer clues than average

This information guides strategic clue placement during refinement.

### Symmetry Preservation

When puzzles have rotational symmetry, the refiner can maintain it by:
- Removing symmetrical pairs of clues together
- Adding clues at symmetrical positions

## Research Background

This approach is based on the paper:

> "Rating and Generating Sudoku Puzzles Based On Constraint Satisfaction Problems"

Key findings:
- Solver iteration count correlates with perceived difficulty
- Iteration ranges were derived from analyzing puzzles from websudoku.com
- The AC3 solver algorithm provides consistent difficulty metrics

## Performance Considerations

- **Refinement overhead**: Iterative refinement requires additional solver calls
- **Caching**: Solver results are cached during refinement to avoid redundant work
- **Early termination**: Refinement stops when puzzle is close enough to target
- **Typical time**: 1-50 refinement iterations depending on initial puzzle

## Tuning Difficulty Ranges

The difficulty ranges can be adjusted in `DifficultyTargets`:

```csharp
private static readonly Dictionary<Difficulty, (int min, int max)> IterationRanges = new()
{
    { Difficulty.Easy, (1, 10) },
    { Difficulty.Medium, (11, 25) },
    { Difficulty.Hard, (26, 80) },
    { Difficulty.Expert, (81, 350) },
    { Difficulty.Evil, (351, int.MaxValue) }
};
```

Adjust these values based on your specific requirements and user feedback.

