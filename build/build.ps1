# Build script for Windows
# Checks for dotnet CLI and builds the solution

param(
    [switch]$SkipTests,
    [switch]$Release,
    [switch]$Publish,
    [string]$Runtime = "",
    [string]$OutputDir = "./publish"
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
$config = if ($Release -or $Publish) { "Release" } else { "Debug" }
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

# Publish if requested
if ($Publish) {
    Write-Host "Publishing application..." -ForegroundColor Yellow
    
    # Determine runtime identifier
    $rid = $Runtime
    if ([string]::IsNullOrEmpty($rid)) {
        # Auto-detect based on current OS
        if ($IsWindows -or $env:OS -eq "Windows_NT") {
            $rid = "win-x64"
        } elseif ($IsMacOS) {
            $rid = "osx-x64"
        } else {
            $rid = "linux-x64"
        }
    }
    
    Write-Host "Target runtime: $rid" -ForegroundColor Cyan
    
    $publishArgs = @(
        "publish",
        "src/SudokuPrintGen.CLI/SudokuPrintGen.CLI/SudokuPrintGen.CLI.csproj",
        "--configuration", "Release",
        "--runtime", $rid,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:EnableCompressionInSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "--output", "$OutputDir/SudokuPrintGen-$rid"
    )
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Publish failed!" -ForegroundColor Red
        exit 1
    }
    
    # Copy additional assets
    $publishDir = "$OutputDir/SudokuPrintGen-$rid"
    
    if (Test-Path "fonts") {
        Write-Host "Copying fonts..." -ForegroundColor Yellow
        Copy-Item -Path "fonts" -Destination "$publishDir/fonts" -Recurse -Force
    }
    
    if (Test-Path "templates") {
        Write-Host "Copying templates..." -ForegroundColor Yellow
        Copy-Item -Path "templates" -Destination "$publishDir/templates" -Recurse -Force
    }
    
    if (Test-Path "config.example.json") {
        Write-Host "Copying config example..." -ForegroundColor Yellow
        Copy-Item -Path "config.example.json" -Destination "$publishDir/" -Force
    }
    
    Write-Host "Published to: $publishDir" -ForegroundColor Green
    Write-Host ""
    
    # Create zip archive
    $zipPath = "$OutputDir/SudokuPrintGen-$rid.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    Write-Host "Creating archive: $zipPath" -ForegroundColor Yellow
    Compress-Archive -Path "$publishDir" -DestinationPath $zipPath
    Write-Host "Archive created successfully" -ForegroundColor Green
    Write-Host ""
}

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host ""

if (-not $Publish) {
    Write-Host "To run the CLI:" -ForegroundColor Cyan
    Write-Host "  dotnet run --project src/SudokuPrintGen.CLI/SudokuPrintGen.CLI -- generate -d Medium" -ForegroundColor White
    Write-Host ""
    Write-Host "To create a release build:" -ForegroundColor Cyan
    Write-Host "  .\build\build.ps1 -Publish -Runtime win-x64" -ForegroundColor White
    Write-Host ""
    Write-Host "Available runtimes: win-x64, linux-x64, osx-x64, osx-arm64" -ForegroundColor White
}
