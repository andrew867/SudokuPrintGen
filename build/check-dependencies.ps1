# Dependency check script for Windows
# Checks for required and optional dependencies

$ErrorActionPreference = "Continue"

Write-Host "SudokuPrintGen Dependency Check" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$allGood = $true

# Check .NET SDK (required)
Write-Host "Checking .NET SDK (required)..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Found: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "  ✗ Not found" -ForegroundColor Red
    Write-Host "    Install: winget install Microsoft.DotNet.SDK.8" -ForegroundColor White
    $allGood = $false
}
Write-Host ""

# Check LaTeX (optional)
Write-Host "Checking LaTeX distribution (optional)..." -ForegroundColor Yellow
$latexFound = $false
$latexEngines = @("xelatex", "pdflatex")
foreach ($engine in $latexEngines) {
    $result = Get-Command $engine -ErrorAction SilentlyContinue
    if ($result) {
        $version = & $engine --version 2>&1 | Select-Object -First 1
        Write-Host "  ✓ Found $engine" -ForegroundColor Green
        $latexFound = $true
    }
}

if (-not $latexFound) {
    Write-Host "  ⚠ Not found (PDF generation will be skipped)" -ForegroundColor Yellow
    Write-Host "    Install: winget install MiKTeX.MiKTeX" -ForegroundColor White
}
Write-Host ""

# Check winget (optional, for installation help)
Write-Host "Checking winget (optional)..." -ForegroundColor Yellow
$wingetVersion = winget --version 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Found: $wingetVersion" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Not found (can't auto-install dependencies)" -ForegroundColor Yellow
}
Write-Host ""

if ($allGood) {
    Write-Host "All required dependencies are installed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some required dependencies are missing." -ForegroundColor Red
    exit 1
}

