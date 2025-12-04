# SudokuPrintGen

Enterprise-grade LaTeX Sudoku generator with high-performance pure C# solver, supporting multiple formats, board sizes, difficulty levels, and variants.

## Features

- **Pure C# Implementation**: High-performance DPLL solver with SIMD-optimized constraint propagation using bit-vector operations
- **Multiple Board Sizes**: 4x4, 6x6, 9x9 (default), 12x12, 16x16
- **Difficulty Levels**: Easy, Medium, Hard, Expert, Evil
- **Variants**: Classic, Diagonal, Color-constrained, Kikagaku
- **Output Formats**: LaTeX (.tex), Plain Text (.txt), JSON, PDF
- **LaTeX Features**: 
  - Support for pdflatex and xelatex engines
  - Custom fonts via fontspec (xetex)
  - Solving sheets (empty grids for working)
  - Metadata (seed, puzzle number, date/time, solver algorithm)
- **Reproducible Generation**: Seed-based puzzle generation (random seed shown if not specified)
- **Press-Ready PDFs**: Professional LaTeX output with embedded fonts and vectors
- **Configuration Files**: JSON-based configuration for batch operations
- **Puzzle Validation**: Automatic validation of generated puzzles
- **Error Handling**: Robust error handling with max retry limits
- **Comprehensive Testing**: 32+ unit and integration tests
- **SIMD Optimizations**: Hardware-accelerated constraint propagation using AVX2/SSE intrinsics
- **Difficulty Rating**: Advanced difficulty analysis based on solving techniques
- **Symmetry Detection**: Automatic detection of rotational, reflectional, and diagonal symmetry
- **Multiple Puzzles Per Page**: Generate 1, 2, 4, 6, or 9 puzzles per LaTeX page

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- LaTeX distribution (optional, for PDF generation):
  - Windows: MikTeX
  - Linux: TeX Live
  - macOS: MacTeX

### Build

**Windows:**
```powershell
.\build\build.ps1
```

**Linux/macOS:**
```bash
./build/build.sh
```

### Usage

Generate a medium difficulty puzzle:
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Medium
```

Generate with a specific seed:
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Hard --seed 42
```

Generate with solving sheet and solution:
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Expert --solving-sheet --solution
```

## Command-Line Options

```
sudoku-printgen generate [options]

Options:
  --size, -s <int>          Board size (4, 6, 9, 12, 16) [default: 9]
  --difficulty, -d <level>   Easy|Medium|Hard|Expert|Evil [default: Medium]
  --variant, -v <type>       Classic|Diagonal|ColorConstrained|Kikagaku [default: Classic]
  --count, -c <int>          Number of puzzles to generate [default: 1]
  --output, -o <path>        Output directory [default: .]
  --format, -f <type>        Tex|Txt|Pdf|Json|All [default: All]
  --engine <engine>          pdflatex|xelatex [default: xelatex]
  --font <name>              Font family name (xetex only)
  --title <text>             Puzzle title
  --author <text>            Author name
  --seed <int>               Random seed for reproducibility (if not specified, random seed is generated and displayed)
  --solution                 Include solution in output
  --solving-sheet            Include solving sheet (empty grid)
  --puzzles-per-page <int>   Number of puzzles per page (1, 2, 4, 6, or 9) [default: 1]
  --config <path>            Configuration file (JSON) - see config.example.json
```

## Examples

Generate 5 easy puzzles with solutions (combined in one LaTeX document):
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Easy -c 5 --solution
```

This creates one LaTeX file with 5 puzzles (4 on first page, 1 on second page), plus individual TXT and JSON files for each puzzle.

Generate a hard puzzle with custom font:
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Hard --font "Times New Roman" --engine xelatex
```

Generate JSON only:
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -f Json -d Medium
```

Generate using configuration file:
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate --config config.example.json
```

## Output Files

Generated files include:
- `sudoku_{difficulty}_{seed}.txt` - Plain text format (81-char string)
- `sudoku_{difficulty}_{seed}.tex` - LaTeX source
- `sudoku_{difficulty}_{seed}.pdf` - Compiled PDF (if LaTeX engine available)
- `sudoku_{difficulty}_{seed}.json` - JSON with puzzle, solution, and metadata

## Metadata

All outputs include metadata:
- **Seed**: Random seed used for generation (for reproducibility)
- **Puzzle Number**: Sequential puzzle number
- **Generated At**: UTC timestamp
- **Solver Algorithm**: "DPLL" (Davis-Putnam-Logemann-Loveland)
- **Difficulty**: Puzzle difficulty level
- **Variant**: Puzzle variant type

## Architecture

- **Core Library** (`SudokuPrintGen.Core`): Puzzle engine, solver, generator, LaTeX output
- **CLI Application** (`SudokuPrintGen.CLI`): Command-line interface
- **Tests** (`SudokuPrintGen.Tests`): Unit tests

## Solver Performance

The solver uses:
- **DPLL Algorithm**: Davis-Putnam-Logemann-Loveland with constraint propagation
- **SIMD Optimizations**: 
  - Hardware-accelerated operations using `System.Runtime.Intrinsics` (AVX2/SSE)
  - Vectorized candidate initialization and elimination
  - Bit-vector operations for efficient candidate tracking
  - Automatic fallback to scalar operations when SIMD unavailable
- **Pure C#**: No native dependencies, fully cross-platform

## Advanced Features

### Difficulty Rating
Puzzles are automatically analyzed to determine:
- Clue count and empty cell ratio
- Required solving techniques (Naked Singles, Hidden Singles, Advanced)
- Estimated difficulty level

### Symmetry Detection
The generator detects and reports:
- Rotational symmetry (180-degree)
- Horizontal and vertical reflection
- Diagonal symmetry
- Overall symmetry score

### Multiple Puzzles Per Page
When generating multiple puzzles (count > 1), they are automatically combined into one LaTeX document:
- **Layout**: 2 puzzles side by side per row
- **Max per page**: 4 puzzles (2 rows × 2 columns)
- **Paper size**: Letter (8.5" × 11")
- **Automatic pagination**: Additional puzzles continue on new pages
- **Single document**: All puzzles in one `.tex` file (and one `.pdf` when compiled)

Example: Generating 6 puzzles creates 2 pages (4 on first page, 2 on second page) in one document.

## Testing

Run all tests:
```bash
dotnet test
```

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]

