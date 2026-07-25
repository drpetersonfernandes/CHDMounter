param(
    [string]$Filter = "",
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

$ErrorActionPreference = "Continue"
$testProject = "SimpleChdDrive.Core.Tests\SimpleChdDrive.Core.Tests.csproj"

$consoles = [ordered]@{
    'MountAndParsePs1Filesystem'             = 'PS1'
    'MountAndParsePs2Filesystem'             = 'PS2'
    'MountAndParsePs3Filesystem'             = 'PS3'
    'MountAndParsePspFilesystem'             = 'PSP'
    'MountAndParseDreamcastFilesystem'       = 'Dreamcast'
    'MountAndParseSaturnFilesystem'          = 'Saturn'
    'MountAndParseXboxFilesystem'            = 'Xbox'
    'MountAndParseThreeDoFilesystem'         = '3DO'
    'MountAndParseCdiFilesystem'             = 'CD-i'
    'MountAndParseNeoGeoCdFilesystem'        = 'Neo Geo CD'
    'MountAndParsePcFxFilesystem'            = 'PC-FX'
    'MountAndParsePc98Filesystem'            = 'PC-98'
    'MountAndParseFmTownsFilesystem'         = 'FM Towns'
    'MountAndParseAmigaCdFilesystem'         = 'Amiga CD'
    'MountAndParseAmigaCd32Filesystem'       = 'Amiga CD32'
    'MountAndParseSegaGenesisCdFilesystem'   = 'Sega Genesis CD'
    'MountAndParsePceCdFilesystem'           = 'PC Engine CD'
    'MountAndParseX68000Filesystem'          = 'X68000'
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Filesystem Parsing Test Runner" -ForegroundColor Cyan
Write-Host "  20 CHDs per console" -ForegroundColor Cyan
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

$filterExpr = "FullyQualifiedName~FilesystemParsingTests"
if ($Filter) {
    $filterExpr = "FullyQualifiedName~$Filter"
}

$argList.Add("--filter")
$argList.Add($filterExpr)

Write-Host "Configuration : $Configuration" -ForegroundColor Gray
Write-Host "Filter        : $filterExpr" -ForegroundColor Gray
Write-Host ""

$sw = [System.Diagnostics.Stopwatch]::StartNew()

$rawOutput = & dotnet $argList 2>&1
$exitCode = $LASTEXITCODE

$sw.Stop()

$output = ($rawOutput | ForEach-Object { "$_" }) -join "`n"
$outputLines = $output -split "`r`n|`n"

$results = [ordered]@{}

foreach ($line in $outputLines) {
    $trimmed = $line.Trim()

    if ($trimmed -match "^Passed (.+?) \[") {
        $name = $Matches[1]
        $console = "Other"
        foreach ($k in $consoles.Keys) {
            if ($name -match "\.$k") { $console = $consoles[$k]; break }
        }
        if (-not $results.Contains($console)) {
            $results[$console] = [PSCustomObject]@{
                Console = $console
                Passed  = [System.Collections.Generic.List[string]]::new()
                Failed  = [System.Collections.Generic.List[string]]::new()
                Skipped = [System.Collections.Generic.List[string]]::new()
            }
        }
        $results[$console].Passed.Add($name)
    }
    elseif ($trimmed -match "^Failed (.+?) \[") {
        $name = $Matches[1]
        $console = "Other"
        foreach ($k in $consoles.Keys) {
            if ($name -match "\.$k") { $console = $consoles[$k]; break }
        }
        if (-not $results.Contains($console)) {
            $results[$console] = [PSCustomObject]@{
                Console = $console
                Passed  = [System.Collections.Generic.List[string]]::new()
                Failed  = [System.Collections.Generic.List[string]]::new()
                Skipped = [System.Collections.Generic.List[string]]::new()
            }
        }
        $results[$console].Failed.Add($name)
    }
    elseif ($trimmed -match "^Skipped (.+?) \[") {
        $name = $Matches[1]
        $console = "Other"
        foreach ($k in $consoles.Keys) {
            if ($name -match "\.$k") { $console = $consoles[$k]; break }
        }
        if (-not $results.Contains($console)) {
            $results[$console] = [PSCustomObject]@{
                Console = $console
                Passed  = [System.Collections.Generic.List[string]]::new()
                Failed  = [System.Collections.Generic.List[string]]::new()
                Skipped = [System.Collections.Generic.List[string]]::new()
            }
        }
        $results[$console].Skipped.Add($name)
    }
}

$totalPassed = ($results.Values | ForEach-Object { $_.Passed.Count } | Measure-Object -Sum).Sum
$totalFailed = ($results.Values | ForEach-Object { $_.Failed.Count } | Measure-Object -Sum).Sum
$totalSkipped = ($results.Values | ForEach-Object { $_.Skipped.Count } | Measure-Object -Sum).Sum
$totalAll = $totalPassed + $totalFailed + $totalSkipped

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  RESULTS BY CONSOLE" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host ("  {0,-20} {1,6} {2,6} {3,6} {4,7}" -f "Console", "Total", "Pass", "Fail", "Skip") -ForegroundColor White
Write-Host ("  {0,-20} {1,6} {2,6} {3,6} {4,7}" -f ("-" * 20), ("-" * 6), ("-" * 6), ("-" * 6), ("-" * 7)) -ForegroundColor DarkGray

$allConsoles = $results.Keys | Sort-Object

foreach ($console in $allConsoles) {
    $r = $results[$console]
    $t = $r.Passed.Count + $r.Failed.Count + $r.Skipped.Count
    $color = if ($r.Failed.Count -eq 0) { "Green" } else { "Red" }
    Write-Host ("  {0,-20} {1,6} {2,6} {3,6} {4,7}" -f $console, $t, $r.Passed.Count, $r.Failed.Count, $r.Skipped.Count) -ForegroundColor $color
}

Write-Host ("  {0,-20} {1,6} {2,6} {3,6} {4,7}" -f ("-" * 20), ("-" * 6), ("-" * 6), ("-" * 6), ("-" * 7)) -ForegroundColor DarkGray
Write-Host ("  {0,-20} {1,6} {2,6} {3,6} {4,7}" -f "TOTAL", $totalAll, $totalPassed, $totalFailed, $totalSkipped) -ForegroundColor White
Write-Host ""

if ($totalFailed -gt 0) {
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "  FAILURES" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""

    foreach ($console in $allConsoles) {
        $r = $results[$console]
        if ($r.Failed.Count -eq 0) { continue }

        Write-Host "--- $console ---" -ForegroundColor Red
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
    foreach ($console in $allConsoles) {
        $r = $results[$console]
        if ($r.Skipped.Count -eq 0) { continue }
        foreach ($s in $r.Skipped) {
            $shortName = $s -replace '^.+\.(.+?\..+)$', '$1'
            Write-Host "  [SKIP] $shortName" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ("  Elapsed: {0:N1}s" -f $sw.Elapsed.TotalSeconds) -ForegroundColor Gray
Write-Host "======================================" -ForegroundColor Cyan

if ($totalFailed -eq 0 -and $totalSkipped -eq 0) {
    Write-Host ""
    Write-Host "  All filesystem parsing tests passed!" -ForegroundColor Green
    Write-Host ""
}
elseif ($totalFailed -eq 0) {
    Write-Host ""
    Write-Host "  All tests passed ($totalSkipped skipped)" -ForegroundColor Yellow
    Write-Host ""
}

exit $exitCode
