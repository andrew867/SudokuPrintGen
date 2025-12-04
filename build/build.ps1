# Build script for Windows
# Checks for dotnet CLI and builds the solution

param(
    [switch]$SkipTests,
    [switch]$Release
)

$ErrorActionPreference = "Stop"

Write-Host "SudokuPrintGen Build Script" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan
Write-Host ""

# Check for dotnet CLI
Write-Host "Checking for .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET SDK not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "To install .NET SDK:" -ForegroundColor Yellow
    Write-Host "  1. Download from https://dotnet.microsoft.com/download" -ForegroundColor White
    Write-Host "  2. Or use winget: winget install Microsoft.DotNet.SDK.8" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "Found .NET SDK: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Check for LaTeX (optional)
Write-Host "Checking for LaTeX distribution..." -ForegroundColor Yellow
$latexFound = $false
$latexEngines = @("xelatex", "pdflatex")
foreach ($engine in $latexEngines) {
    $result = Get-Command $engine -ErrorAction SilentlyContinue
    if ($result) {
        Write-Host "Found $engine" -ForegroundColor Green
        $latexFound = $true
    }
}

if (-not $latexFound) {
    Write-Host "WARNING: LaTeX not found. PDF generation will be skipped." -ForegroundColor Yellow
    Write-Host "To install MikTeX:" -ForegroundColor Yellow
    Write-Host "  1. Download from https://miktex.org/download" -ForegroundColor White
    Write-Host "  2. Or use winget: winget install MiKTeX.MiKTeX" -ForegroundColor White
    Write-Host ""
}

Write-Host ""

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Packages restored successfully" -ForegroundColor Green
Write-Host ""

# Build solution
$config = if ($Release) { "Release" } else { "Debug" }
Write-Host "Building solution (Configuration: $config)..." -ForegroundColor Yellow
dotnet build --configuration $config
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build succeeded" -ForegroundColor Green
Write-Host ""

# Run tests
if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --no-build --configuration $config
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Tests failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "All tests passed" -ForegroundColor Green
    Write-Host ""
}

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To run the CLI:" -ForegroundColor Cyan
Write-Host "  dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Medium" -ForegroundColor White

