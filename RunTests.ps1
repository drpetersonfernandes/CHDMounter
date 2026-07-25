param(
    [string]$Filter = "",
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

$ErrorActionPreference = "Continue"
$testProject = "SimpleChdDrive.Core.Tests\SimpleChdDrive.Core.Tests.csproj"

$parsingTests = [ordered]@{
    'AmigaCd32IntegrationTests'     = 'Amiga CD32'
    'AmigaCdIntegrationTests'       = 'Amiga CD'
    'CDiIntegrationTests'           = 'CD-i'
    'DreamcastIntegrationTests'     = 'Dreamcast'
    'FmTownsIntegrationTests'       = 'FM Towns'
    'NeoGeoCdIntegrationTests'      = 'Neo Geo CD'
    'PceCdIntegrationTests'         = 'PC Engine CD'
    'PcFxIntegrationTests'          = 'PC-FX'
    'Pc98IntegrationTests'          = 'PC-98'
    'Ps1IntegrationTests'           = 'PS1'
    'Ps2IntegrationTests'           = 'PS2'
    'Ps3IntegrationTests'           = 'PS3'
    'PspIntegrationTests'           = 'PSP'
    'SaturnIntegrationTests'        = 'Saturn'
    'SegaGenesisCdIntegrationTests' = 'Sega Genesis CD'
    'ThreeDoIntegrationTests'       = '3DO'
    'XboxIntegrationTests'          = 'Xbox'
    'X68000IntegrationTests'        = 'X68000'
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  SimpleChdDrive Parsing Test Runner" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

$argList = [System.Collections.Generic.List[string]]@(
    "test", $testProject,
    "-c", $Configuration,
    "--verbosity", "normal",
    "--nologo"
)

if ($NoBuild) {
    $argList.Add("--no-build")
}

$filterExpr = "FullyQualifiedName~IntegrationTests"
if ($Filter) {
    $filterExpr = "FullyQualifiedName~$Filter"
    Write-Host "Filter: $Filter" -ForegroundColor Gray
}

$argList.Add("--filter")
$argList.Add($filterExpr)

Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Filter: $filterExpr" -ForegroundColor Gray
Write-Host ""
Write-Host "Running parsing tests..." -ForegroundColor Yellow
Write-Host ""

$rawOutput = & dotnet $argList 2>&1
$exitCode = $LASTEXITCODE

$output = ($rawOutput | ForEach-Object { "$_" }) -join "`n"
$outputLines = $output -split "`r`n|`n"

Write-Host ""

$results = @{}

foreach ($line in $outputLines) {
    $trimmed = $line.Trim()

    if ($trimmed -match "^Passed (.+?) \[") {
        $name = $Matches[1]
        $system = "Other"
        foreach ($k in $parsingTests.Keys) {
            if ($name -match "\.$k\.") { $system = $parsingTests[$k]; break }
        }
        if (-not $results.ContainsKey($system)) {
            $results[$system] = [PSCustomObject]@{
                System  = $system
                Passed  = [System.Collections.Generic.List[string]]::new()
                Failed  = [System.Collections.Generic.List[string]]::new()
                Skipped = [System.Collections.Generic.List[string]]::new()
            }
        }
        $results[$system].Passed.Add($name)
    }
    elseif ($trimmed -match "^Failed (.+?) \[") {
        $name = $Matches[1]
        $system = "Other"
        foreach ($k in $parsingTests.Keys) {
            if ($name -match "\.$k\.") { $system = $parsingTests[$k]; break }
        }
        if (-not $results.ContainsKey($system)) {
            $results[$system] = [PSCustomObject]@{
                System  = $system
                Passed  = [System.Collections.Generic.List[string]]::new()
                Failed  = [System.Collections.Generic.List[string]]::new()
                Skipped = [System.Collections.Generic.List[string]]::new()
            }
        }
        $results[$system].Failed.Add($name)
    }
    elseif ($trimmed -match "^Skipped (.+?) \[") {
        $name = $Matches[1]
        $system = "Other"
        foreach ($k in $parsingTests.Keys) {
            if ($name -match "\.$k\.") { $system = $parsingTests[$k]; break }
        }
        if (-not $results.ContainsKey($system)) {
            $results[$system] = [PSCustomObject]@{
                System  = $system
                Passed  = [System.Collections.Generic.List[string]]::new()
                Failed  = [System.Collections.Generic.List[string]]::new()
                Skipped = [System.Collections.Generic.List[string]]::new()
            }
        }
        $results[$system].Skipped.Add($name)
    }
}

$totalPassed = 0
$totalFailed = 0
$totalSkipped = 0

foreach ($r in $results.Values) {
    $totalPassed += $r.Passed.Count
    $totalFailed += $r.Failed.Count
    $totalSkipped += $r.Skipped.Count
}

$totalAll = $totalPassed + $totalFailed + $totalSkipped

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  RESULTS BY CONSOLE" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f "Console", "Total", "Pass", "Fail", "Skip") -ForegroundColor White
Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f ("-" * 22), ("-" * 6), ("-" * 6), ("-" * 6), ("-" * 7)) -ForegroundColor DarkGray

$allSystems = $results.Keys | Sort-Object

foreach ($system in $allSystems) {
    $r = $results[$system]
    $t = $r.Passed.Count + $r.Failed.Count + $r.Skipped.Count
    $color = if ($r.Failed.Count -eq 0) { "Green" } else { "Red" }
    Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f $system, $t, $r.Passed.Count, $r.Failed.Count, $r.Skipped.Count) -ForegroundColor $color
}

Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f ("-" * 22), ("-" * 6), ("-" * 6), ("-" * 6), ("-" * 7)) -ForegroundColor DarkGray
Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f "TOTAL", $totalAll, $totalPassed, $totalFailed, $totalSkipped) -ForegroundColor White
Write-Host ""

if ($totalFailed -gt 0) {
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "  FAILURES" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""

    foreach ($system in $allSystems) {
        $r = $results[$system]
        if ($r.Failed.Count -eq 0) { continue }

        Write-Host "--- $system ---" -ForegroundColor Red
        foreach ($f in $r.Failed) {
            $shortName = $f -replace '^.+\.(.+?\..+)$', '$1'
            Write-Host "  [FAIL] $shortName" -ForegroundColor Red
        }
        Write-Host ""
    }

    Write-Host "--- FAILURE DETAILS ---" -ForegroundColor Red
    $inFailure = $false
    foreach ($line in $outputLines) {
        $trimmed = $line.Trim()
        if ($trimmed -match "^Failed (.+?) \[") {
            $inFailure = $true
            Write-Host ""
            $shortName = $Matches[1] -replace '^.+\.(.+?\..+)$', '$1'
            Write-Host "  $shortName" -ForegroundColor Red
        }
        elseif ($inFailure -and $trimmed -match "^(Passed |Skipped |Total tests:|^  )" -and $trimmed -notmatch "Error|Stack|at |   at ") {
            $inFailure = $false
        }
        elseif ($inFailure -and $trimmed -match "(Error Message:|Stack Trace:|^\s+at |^\s+->)") {
            Write-Host "    $trimmed" -ForegroundColor DarkRed
        }
        elseif ($inFailure -and $trimmed -ne "" -and $trimmed -notmatch "^Failed ") {
            Write-Host "    $trimmed" -ForegroundColor DarkRed
        }
    }
}

if ($totalSkipped -gt 0) {
    Write-Host ""
    Write-Host "--- SKIPPED ---" -ForegroundColor Yellow
    foreach ($system in $allSystems) {
        $r = $results[$system]
        if ($r.Skipped.Count -eq 0) { continue }
        foreach ($s in $r.Skipped) {
            $shortName = $s -replace '^.+\.(.+?\..+)$', '$1'
            Write-Host "  [SKIP] $shortName" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan

if ($totalFailed -eq 0 -and $totalSkipped -eq 0) {
    Write-Host ""
    Write-Host "  All parsing tests passed!" -ForegroundColor Green
    Write-Host ""
}
elseif ($totalFailed -eq 0) {
    Write-Host ""
    Write-Host "  All tests passed ($totalSkipped skipped)" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "======================================" -ForegroundColor Cyan

exit $exitCode
