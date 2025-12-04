# 🧩 SudokuPrintGen

**Enterprise-grade LaTeX Sudoku Generator** - Beautiful, press-ready PDFs with a blazing-fast SIMD-optimized solver.

Built with .NET 8 C# and TikZ graphics, SudokuPrintGen produces publication-quality output suitable for books, magazines, and print media.

---

## ✨ Key Features

### 🚀 High-Performance Puzzle Generation
- **Pure C# SIMD-optimized DPLL solver** using `System.Runtime.Intrinsics` (AVX2/SSE)
- **Multiple difficulty levels**: Easy, Medium, Hard, Expert, Evil
- **Puzzle variants**: Classic, Diagonal, Color-Constrained, Kikagaku
- **Configurable board sizes**: 4×4, 6×6, 9×9, 12×12, 16×16
- **Reproducible generation** with seed support
- **Difficulty rating algorithm** based on solving techniques
- **Symmetry detection** (rotational, horizontal, vertical, diagonal)

### 📄 Multiple Output Formats
- **LaTeX (.tex)** - Full source for customization
- **PDF** - Beautiful press-ready output via XeLaTeX
- **TXT** - Simple 81-character puzzle strings
- **JSON** - Structured data with full metadata

### 🎨 Beautiful PDF Generation
- **TikZ graphics** for precise, vector-based grids
- **Perfectly square cells** with consistent line weights
- **Thin gray lines** (0.3pt) for cell borders
- **Thick black lines** (1.2pt) for 3×3 box borders
- **Bundled Futura Bold BT font** (auto-copied during build)
- **Custom font support** (TTF files or system fonts)
- **Multi-puzzle layouts**: 8 puzzles per page (2 columns × 4 rows)
- **Optimized for Letter size** (8.5" × 11") paper

### 📋 Multi-Puzzle Features
- Batch generation with **mixed difficulties**
- **Automatic page balancing**
- Individual puzzle footers (number, seed, date, difficulty)
- Combined LaTeX/PDF output with timestamp naming

---

## 🚀 Quick Start

### Prerequisites

- **.NET 8.0 SDK**
- **LaTeX distribution** (for PDF generation):
  - Windows: [MikTeX](https://miktex.org/)
  - Linux: TeX Live (`sudo apt install texlive-xetex`)
  - macOS: [MacTeX](https://www.tug.org/mactex/)

### Build

**Windows:**
```powershell
.\build\build.ps1
```

**Linux/macOS:**
```bash
./build/build.sh
```

**Or simply:**
```bash
dotnet build
```

---

## 📖 Usage Examples

### Generate 8 medium puzzles as PDF
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -n 8 -d Medium -f pdf
```

### Mixed difficulties with custom seed
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -n 12 -d Easy,Medium,Hard --seed 12345
```

### Use system font instead of bundled
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -n 4 --system-font Arial -f pdf
```

### Generate all formats
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -n 1 -d Expert -f all
```

### Generate with solving sheet and solution
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Hard --solving-sheet --solution
```

### Use configuration file
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate --config config.example.json
```

---

## ⚙️ Command-Line Options

```
sudoku-printgen generate [options]

Options:
  --size, -s <int>           Board size (4, 6, 9, 12, 16) [default: 9]
  --difficulty, -d <level>   Easy|Medium|Hard|Expert|Evil [default: Medium]
                             (comma-separated for mixed: Easy,Medium,Hard)
  --variant, -v <type>       Classic|Diagonal|ColorConstrained|Kikagaku [default: Classic]
  --count, -n, -c <int>      Number of puzzles to generate [default: 1]
  --output, -o <path>        Output directory [default: .]
  --format, -f <type>        tex|txt|pdf|json|all [default: all]
  --engine <engine>          pdflatex|xelatex [default: xelatex]
  --font <path>              Path to TTF font file (xelatex only)
  --system-font <name>       Use installed system font by name (xelatex only)
  --no-bundled-font          Don't use bundled Futura Bold BT font
  --title <text>             Puzzle title
  --author <text>            Author name
  --seed <int>               Random seed for reproducibility
  --solution                 Include solution in output
  --solving-sheet            Include solving sheet (empty grid)
  --config <path>            Configuration file (JSON)

Difficulty Targeting Options:
  --refine-difficulty        Use iterative refinement for accurate difficulty targeting
  --show-statistics          Display generation statistics after completion
  --verbose                  Show detailed progress during generation
```

---

## 📁 Project Structure

```
SudokuPrintGen/
├── src/
│   ├── SudokuPrintGen.Core/     # Core library
│   │   ├── Puzzle/              # Board, Generator, DifficultyRater
│   │   ├── Solver/              # DPLL solver with SIMD optimizations
│   │   ├── LaTeX/               # TikZ-based grid generation
│   │   └── Output/              # Multi-format writers, PDF compiler
│   └── SudokuPrintGen.CLI/      # Command-line interface
├── tests/
│   └── SudokuPrintGen.Tests/    # 40 unit tests
├── fonts/                        # Bundled Futura fonts
├── templates/latex/              # LaTeX templates
├── build/                        # Build scripts (PowerShell & Bash)
└── docs/                         # Documentation
```

---

## 🛠️ Technical Architecture

### Core Library Components

| Component | Description |
|-----------|-------------|
| `Board.cs` | Sudoku board representation with clone/validation |
| `Generator.cs` | Puzzle generation with SIMD-optimized solver |
| `DpllSolver.cs` | Davis-Putnam-Logemann-Loveland algorithm with metrics |
| `SolverResult.cs` | Solver metrics (iterations, depth, guesses) |
| `SimdConstraintPropagator.cs` | AVX2/SSE hardware-accelerated operations |
| `DifficultyRater.cs` | Metrics-based difficulty analysis |
| `DifficultyTargets.cs` | Iteration ranges and score thresholds |
| `PuzzleRefiner.cs` | Iterative difficulty refinement |
| `ClueAnalyzer.cs` | Strategic clue management |
| `GenerationStatistics.cs` | Batch generation statistics tracking |
| `SymmetryDetector.cs` | Pattern detection algorithms |
| `LaTeXGenerator.cs` | TikZ-based grid generation |
| `PdfCompiler.cs` | XeLaTeX/pdfLaTeX compilation |

### Solver Performance

The solver uses cutting-edge optimizations:
- **DPLL Algorithm** with constraint propagation
- **SIMD Intrinsics** via `System.Runtime.Intrinsics`:
  - Vectorized candidate initialization and elimination
  - Hardware `PopCount` for efficient bit counting
  - Automatic fallback to scalar operations when unavailable
- **Bit-vector operations** for efficient candidate tracking
- **Pure C#** - No native dependencies, fully cross-platform

---

## 📊 Output Files

Generated files follow this naming convention:

| Format | Filename | Description |
|--------|----------|-------------|
| TXT | `sudoku_{difficulty}_seed_{seed}.txt` | 81-character puzzle string |
| JSON | `sudoku_{difficulty}_seed_{seed}.json` | Full metadata + puzzle/solution arrays |
| LaTeX | `sudoku_combined_{timestamp}.tex` | TikZ source (multi-puzzle) |
| PDF | `sudoku_combined_{timestamp}.pdf` | Press-ready output |

### JSON Metadata Includes:
- Seed, puzzle number, generation timestamp
- Difficulty level and variant type
- Solver algorithm ("DPLL")
- Difficulty rating (clue count, techniques required)
- Symmetry analysis (types detected, score)

---

## 🎯 Advanced Features

### Iteration-Based Difficulty System

SudokuPrintGen uses a research-backed approach to difficulty targeting based on solver metrics:

| Difficulty | Iteration Range | Description |
|------------|-----------------|-------------|
| Easy       | 1-10            | Minimal backtracking required |
| Medium     | 11-25           | Some deduction needed |
| Hard       | 26-80           | Advanced techniques required |
| Expert     | 81-350          | Significant backtracking |
| Evil       | 351+            | Maximum difficulty |

#### Iterative Refinement
Enable with `--refine-difficulty` for accurate targeting:
```bash
dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Hard --refine-difficulty
```

The system:
1. Generates initial puzzle with approximate clue count
2. Measures actual difficulty using solver metrics
3. Iteratively adjusts clues until difficulty matches target

#### Statistics Report
Enable with `--show-statistics`:
```
=== Generation Statistics Report ===

Difficulty    | Count | Avg Iter | Std Dev | Success % | Avg Score
--------------|-------|----------|---------|-----------|----------
Medium        |     5 |     17.4 |    3.21 |      80.0% |     14.2
```

See [docs/DifficultySystem.md](docs/DifficultySystem.md) for complete documentation.

### Difficulty Rating
Puzzles are automatically analyzed to determine:
- Solver iteration count (primary metric)
- Max backtrack depth and guess count
- Clue count and empty cell ratio
- Required solving techniques (Naked Singles, Hidden Singles, Advanced)
- Composite difficulty score
- Estimated difficulty level

### Symmetry Detection
The generator detects and reports:
- Rotational symmetry (180-degree)
- Horizontal and vertical reflection
- Diagonal symmetry
- Overall symmetry score

### Multi-Puzzle Layouts
When generating multiple puzzles:
- **Layout**: 2 columns × 4 rows = 8 puzzles per page
- **Paper size**: Letter (8.5" × 11")
- **Individual footers**: Each puzzle shows its number, seed, date, difficulty
- **Automatic pagination**: Additional puzzles continue on new pages
- **Balanced distribution**: Puzzles distributed evenly across pages

---

## ✅ Testing

Run all tests:
```bash
dotnet test
```

Test coverage includes:
- Board operations and validation
- Solver correctness and uniqueness checking
- Solver metrics tracking (iterations, depth, guesses)
- Generator with various difficulties
- Difficulty rating algorithms
- Difficulty targets and ranges
- Iterative refinement process
- Generation statistics tracking
- Symmetry detection
- PDF compilation
- Difficulty distribution for mixed batches

---

## 🎯 Future Roadmap

- [ ] Web API / REST interface
- [ ] GUI application (WPF/Avalonia)
- [ ] Additional puzzle variants
- [ ] More export formats (SVG, PNG)
- [ ] Batch processing CLI improvements
- [ ] Puzzle book generation mode

---

## 📜 License

MIT License - See [LICENSE](LICENSE) for details.

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

Built with ❤️ for puzzle enthusiasts and publishers.
