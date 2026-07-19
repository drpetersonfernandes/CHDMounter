param(
    [string]$Filter = "",
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

$ErrorActionPreference = "Continue"
$testProject = "SimpleChdDrive.Core.Tests\SimpleChdDrive.Core.Tests.csproj"

$systemMap = @{
    'Ps1IntegrationTests'           = 'PlayStation 1'
    'Ps2IntegrationTests'           = 'PlayStation 2'
    'Ps3IntegrationTests'           = 'PlayStation 3'
    'PspIntegrationTests'           = 'PlayStation Portable'
    'AmigaCd32IntegrationTests'     = 'Amiga CD32'
    'AmigaCdIntegrationTests'       = 'Amiga CD'
    'CDiIntegrationTests'           = 'CD-i'
    'CDiDiagnosticTests'            = 'CD-i'
    'DreamcastIntegrationTests'     = 'Dreamcast'
    'FmTownsIntegrationTests'       = 'FM Towns'
    'PceCdIntegrationTests'         = 'PC Engine CD'
    'PcFxIntegrationTests'          = 'PC-FX'
    'PcFxDiagnosticTests'           = 'PC-FX'
    'Pc98IntegrationTests'          = 'PC-98'
    'ThreeDoIntegrationTests'       = '3DO'
    'XboxIntegrationTests'           = 'Xbox'
    'Xbox360IntegrationTests'       = 'Xbox 360'
    'X68000IntegrationTests'        = 'X68000'
    'ServiceProviderTests'          = 'Services'
    'ConsoleInfoTests'              = 'Models'
    'ConsoleTypeTests'              = 'Models'
    'FileEntryTests'                = 'Models'
    'FsNodeTests'                   = 'Models'
    'LogEntryTests'                 = 'Models'
    'TrackInfoTests'                = 'Models'
    'ParserFactoryTests'            = 'Parser Factory'
}

function EnsureGroup($results, $system) {
    if (-not $results.ContainsKey($system)) {
        $results[$system] = [PSCustomObject]@{
            System  = $system
            Total   = 0
            Passed  = [System.Collections.Generic.List[string]]::new()
            Failed  = [System.Collections.Generic.List[string]]::new()
            Skipped = [System.Collections.Generic.List[string]]::new()
        }
    }
}

Write-Host "==============================" -ForegroundColor Cyan
Write-Host "  SimpleChdDrive Test Runner" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
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

if ($Filter) {
    $argList.Add("--filter")
    $argList.Add($Filter)
    Write-Host "Filter: $Filter" -ForegroundColor Gray
}

Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Test project: $testProject" -ForegroundColor Gray
Write-Host ""

Write-Host "Running tests..." -ForegroundColor Yellow
Write-Host ""

$rawOutput = & dotnet $argList 2>&1
$exitCode = $LASTEXITCODE

$output = ($rawOutput | ForEach-Object { "$_" }) -join "`n"
$outputLines = $output -split "`r`n|`n"

Write-Host ""

$results = @{}
$testTotal = 0

foreach ($line in $outputLines) {
    $trimmed = $line.Trim()
    switch -Regex ($trimmed) {
        "^Passed (.+?) \[" {
            $name = $Matches[1]
            $system = "Other"
            foreach ($k in $systemMap.Keys) {
                if ($name -match "\.$k\.") { $system = $systemMap[$k]; break }
            }
            EnsureGroup $results $system
            $results[$system].Passed.Add($name)
            $results[$system].Total++
            $testTotal++
        }
        "^Failed (.+?) \[" {
            $name = $Matches[1]
            $system = "Other"
            foreach ($k in $systemMap.Keys) {
                if ($name -match "\.$k\.") { $system = $systemMap[$k]; break }
            }
            EnsureGroup $results $system
            $results[$system].Failed.Add($name)
            $results[$system].Total++
            $testTotal++
        }
        "^Skipped (.+?) \[" {
            $name = $Matches[1]
            $system = "Other"
            foreach ($k in $systemMap.Keys) {
                if ($name -match "\.$k\.") { $system = $systemMap[$k]; break }
            }
            EnsureGroup $results $system
            $results[$system].Skipped.Add($name)
            $results[$system].Total++
            $testTotal++
        }
        "^Total tests: (\d+)" {
            $testTotal = [int]$Matches[1]
        }
    }
}

$allSystems = $results.Keys | Sort-Object

Write-Host "==============================" -ForegroundColor Cyan
Write-Host "  TEST RESULTS BY SYSTEM" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""
Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f "System", "Total", "Passed", "Failed", "Skipped") -ForegroundColor White
Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f ("-" * 22), ("-" * 6), ("-" * 6), ("-" * 6), ("-" * 7)) -ForegroundColor DarkGray

$totalPassed = 0
$totalFailed = 0
$totalSkipped = 0

foreach ($system in $allSystems) {
    $r = $results[$system]
    $totalPassed += $r.Passed.Count
    $totalFailed += $r.Failed.Count
    $totalSkipped += $r.Skipped.Count

    $color = if ($r.Failed.Count -eq 0) { "Green" } else { "Red" }
    Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f $system, $r.Total, $r.Passed.Count, $r.Failed.Count, $r.Skipped.Count) -ForegroundColor $color
}

Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f ("-" * 22), ("-" * 6), ("-" * 6), ("-" * 6), ("-" * 7)) -ForegroundColor DarkGray
Write-Host ("  {0,-22} {1,6} {2,6} {3,6} {4,7}" -f "TOTAL", $testTotal, $totalPassed, $totalFailed, $totalSkipped) -ForegroundColor White
Write-Host ""

if ($totalFailed -gt 0) {
    Write-Host "==============================" -ForegroundColor Cyan
    Write-Host "  FAILURES BY SYSTEM" -ForegroundColor Cyan
    Write-Host "==============================" -ForegroundColor Cyan
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
        elseif ($inFailure -and $trimmed -match "^(Passed |Skipped |Total tests:$|^  )" -and $trimmed -notmatch "Error|Stack|at |   at ") {
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
    Write-Host "--- SKIPPED TESTS ---" -ForegroundColor Yellow
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
Write-Host "==============================" -ForegroundColor Cyan

if ($totalFailed -eq 0 -and $totalSkipped -eq 0) {
    Write-Host ""
    Write-Host "  All tests passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "==============================" -ForegroundColor Cyan
}

exit $exitCode
