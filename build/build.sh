#!/bin/bash
# Build script for Linux/macOS
# Checks for dotnet CLI and builds the solution

set -e

SKIP_TESTS=false
RELEASE=false
PUBLISH=false
RUNTIME=""
OUTPUT_DIR="./publish"

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
        --publish)
            PUBLISH=true
            shift
            ;;
        --runtime)
            RUNTIME="$2"
            shift 2
            ;;
        --output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --help|-h)
            echo "Usage: build.sh [options]"
            echo ""
            echo "Options:"
            echo "  --skip-tests    Skip running tests"
            echo "  --release       Build in Release configuration"
            echo "  --publish       Create self-contained publish"
            echo "  --runtime RID   Target runtime (e.g., linux-x64, osx-x64, osx-arm64)"
            echo "  --output DIR    Output directory for publish (default: ./publish)"
            echo "  --help, -h      Show this help message"
            echo ""
            echo "Available runtimes: win-x64, linux-x64, osx-x64, osx-arm64"
            exit 0
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
if [ "$RELEASE" = true ] || [ "$PUBLISH" = true ]; then
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

# Publish if requested
if [ "$PUBLISH" = true ]; then
    echo "Publishing application..."
    
    # Determine runtime identifier
    RID="$RUNTIME"
    if [ -z "$RID" ]; then
        # Auto-detect based on current OS
        case "$(uname -s)" in
            Linux*)
                RID="linux-x64"
                ;;
            Darwin*)
                # Check for ARM vs Intel
                if [ "$(uname -m)" = "arm64" ]; then
                    RID="osx-arm64"
                else
                    RID="osx-x64"
                fi
                ;;
            *)
                RID="linux-x64"
                ;;
        esac
    fi
    
    echo "Target runtime: $RID"
    
    PUBLISH_DIR="$OUTPUT_DIR/SudokuPrintGen-$RID"
    
    dotnet publish src/SudokuPrintGen.CLI/SudokuPrintGen.CLI/SudokuPrintGen.CLI.csproj \
        --configuration Release \
        --runtime "$RID" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:EnableCompressionInSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        --output "$PUBLISH_DIR"
    
    if [ $? -ne 0 ]; then
        echo "ERROR: Publish failed!"
        exit 1
    fi
    
    # Copy additional assets
    if [ -d "fonts" ]; then
        echo "Copying fonts..."
        cp -r fonts "$PUBLISH_DIR/"
    fi
    
    if [ -d "templates" ]; then
        echo "Copying templates..."
        cp -r templates "$PUBLISH_DIR/"
    fi
    
    if [ -f "config.example.json" ]; then
        echo "Copying config example..."
        cp config.example.json "$PUBLISH_DIR/"
    fi
    
    echo "Published to: $PUBLISH_DIR"
    echo ""
    
    # Create zip archive
    ZIP_PATH="$OUTPUT_DIR/SudokuPrintGen-$RID.zip"
    if [ -f "$ZIP_PATH" ]; then
        rm "$ZIP_PATH"
    fi
    
    echo "Creating archive: $ZIP_PATH"
    cd "$OUTPUT_DIR"
    zip -r "SudokuPrintGen-$RID.zip" "SudokuPrintGen-$RID"
    cd - > /dev/null
    echo "Archive created successfully"
    echo ""
fi

echo "Build completed successfully!"
echo ""

if [ "$PUBLISH" = false ]; then
    echo "To run the CLI:"
    echo "  dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Medium"
    echo ""
    echo "To create a release build:"
    echo "  ./build/build.sh --publish --runtime linux-x64"
    echo ""
    echo "Available runtimes: win-x64, linux-x64, osx-x64, osx-arm64"
fi
