param(
    [string]$Filter = "",
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$testProject = "SimpleChdDrive.Core.Tests\SimpleChdDrive.Core.Tests.csproj"

Write-Host "==============================" -ForegroundColor Cyan
Write-Host "  SimpleChdDrive Test Runner" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""

$argList = @(
    "test", $testProject,
    "-c", $Configuration,
    "--verbosity", "normal",
    "--nologo"
)

if ($NoBuild) {
    $argList += "--no-build"
}

if ($Filter) {
    $argList += "--filter"
    $argList += $Filter
    Write-Host "Filter: $Filter" -ForegroundColor Gray
}

Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Test project: $testProject" -ForegroundColor Gray
Write-Host ""

Write-Host "Running tests..." -ForegroundColor Yellow
Write-Host ""

$output = & dotnet $argList 2>&1
$exitCode = $LASTEXITCODE
$outputLines = $output -split "`r`n|`n"

Write-Host ""

$passed = @()
$failed = @()
$skipped = @()
$total = 0
$elapsed = ""

foreach ($line in $outputLines) {
    switch -Regex ($line.Trim()) {
        "^Passed (.+?) \[" {
            $name = $Matches[1]
            $passed += $name
            $total++
        }
        "^Failed (.+?) \[" {
            $name = $Matches[1]
            $failed += $name
            $total++
        }
        "^Skipped (.+?) \[" {
            $name = $Matches[1]
            $skipped += $name
            $total++
        }
        "^Total tests: (\d+)" {
            $total = [int]$Matches[1]
        }
    }
}

Write-Host "==============================" -ForegroundColor Cyan
Write-Host "  TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""

Write-Host ("  Total:   " + $total.ToString().PadLeft(5)) -ForegroundColor White
Write-Host ("  Passed:  " + $passed.Count.ToString().PadLeft(5)) -ForegroundColor Green
Write-Host ("  Failed:  " + $failed.Count.ToString().PadLeft(5)) -ForegroundColor Red
Write-Host ("  Skipped: " + $skipped.Count.ToString().PadLeft(5)) -ForegroundColor Yellow
Write-Host ""

if ($failed.Count -gt 0 -or $skipped.Count -gt 0) {

    Write-Host "==============================" -ForegroundColor Cyan
    Write-Host "  DETAILED REPORT" -ForegroundColor Cyan
    Write-Host "==============================" -ForegroundColor Cyan
    Write-Host ""

    if ($failed.Count -gt 0) {
        Write-Host "--- FAILED TESTS ---" -ForegroundColor Red
        $failed | ForEach-Object { Write-Host "  [FAIL] $_" -ForegroundColor Red }

        Write-Host ""
        Write-Host "--- FAILURE DETAILS ---" -ForegroundColor Red
        $inFailure = $false
        $currentTest = ""
        foreach ($line in $outputLines) {
            $trimmed = $line.Trim()
            if ($trimmed -match "^Failed (.+?) \[") {
                $currentTest = $Matches[1]
                $inFailure = $true
                Write-Host ""
                Write-Host "  $currentTest" -ForegroundColor Red
            }
            elseif ($inFailure -and $trimmed -match "^(Passed |Skipped |Total tests:|$|^  )" -and $trimmed -notmatch "Error|Stack|at |   at ") {
                $inFailure = $false
                $currentTest = ""
            }
            elseif ($inFailure -and $trimmed -match "(Error Message:|Stack Trace:|^\s+at |^\s+->)") {
                Write-Host "    $trimmed" -ForegroundColor DarkRed
            }
            elseif ($inFailure -and $trimmed -ne "" -and $trimmed -notmatch "^Failed ") {
                Write-Host "    $trimmed" -ForegroundColor DarkRed
            }
        }
    }

    if ($skipped.Count -gt 0) {
        Write-Host ""
        Write-Host "--- SKIPPED TESTS ---" -ForegroundColor Yellow
        $skipped | ForEach-Object { Write-Host "  [SKIP] $_" -ForegroundColor Yellow }
    }
}
else {
    Write-Host "All tests passed!" -ForegroundColor Green
}

Write-Host ""
Write-Host "==============================" -ForegroundColor Cyan

Write-Host ""
Write-Host "=== RAW CONSOLE OUTPUT ===" -ForegroundColor DarkGray
Write-Host $output -ForegroundColor DarkGray

exit $exitCode
