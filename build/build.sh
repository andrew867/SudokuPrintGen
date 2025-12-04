#!/bin/bash
# Build script for Linux/macOS
# Checks for dotnet CLI and builds the solution

set -e

SKIP_TESTS=false
RELEASE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --release)
            RELEASE=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "SudokuPrintGen Build Script"
echo "============================"
echo ""

# Check for dotnet CLI
echo "Checking for .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found!"
    echo ""
    echo "To install .NET SDK:"
    echo "  Linux: https://learn.microsoft.com/dotnet/core/install/linux"
    echo "  macOS: https://learn.microsoft.com/dotnet/core/install/macos"
    echo ""
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "Found .NET SDK: $DOTNET_VERSION"
echo ""

# Check for LaTeX (optional)
echo "Checking for LaTeX distribution..."
LATEX_FOUND=false
if command -v xelatex &> /dev/null; then
    echo "Found xelatex"
    LATEX_FOUND=true
fi
if command -v pdflatex &> /dev/null; then
    echo "Found pdflatex"
    LATEX_FOUND=true
fi

if [ "$LATEX_FOUND" = false ]; then
    echo "WARNING: LaTeX not found. PDF generation will be skipped."
    echo "To install TeX Live:"
    echo "  Linux: sudo apt-get install texlive-full (or equivalent)"
    echo "  macOS: brew install --cask mactex"
    echo ""
fi

echo ""

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "ERROR: Package restore failed!"
    exit 1
fi
echo "Packages restored successfully"
echo ""

# Build solution
CONFIG="Debug"
if [ "$RELEASE" = true ]; then
    CONFIG="Release"
fi

echo "Building solution (Configuration: $CONFIG)..."
dotnet build --configuration $CONFIG
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed!"
    exit 1
fi
echo "Build succeeded"
echo ""

# Run tests
if [ "$SKIP_TESTS" = false ]; then
    echo "Running tests..."
    dotnet test --no-build --configuration $CONFIG
    if [ $? -ne 0 ]; then
        echo "ERROR: Tests failed!"
        exit 1
    fi
    echo "All tests passed"
    echo ""
fi

echo "Build completed successfully!"
echo ""
echo "To run the CLI:"
echo "  dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Medium"

