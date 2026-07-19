# SimpleChdDrive Tester

WPF desktop application for **batch testing and benchmarking** CHD disc image parsing. Scans folders of `.chd` files, parses each one with a selected console file system parser, and generates summary reports with PDF export.

[![NuGet](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

## Features

- Select a folder of CHD files and a target console type
- Batch-parse every `.chd` file in the folder
- Report success/failure, file count, directory count, volume size, and timing per file
- View aggregated summary statistics (fastest, slowest, average, total throughput)
- Export results to **PDF** with QuestPDF

## Supported Consoles

Same 20+ console file systems as the main SimpleChdDrive application — see the [main project](https://github.com/drpetersonfernandes/SimpleChdDrive) for the full list.

## Usage

1. Launch `SimpleChdDrive_Tester.exe`
2. Select a folder containing `.chd` files
3. Choose a console type from the dropdown
4. Click **Run Tests** to begin batch parsing
5. Click **Export PDF** to save results

## Architecture

| Component | Description |
|-----------|-------------|
| `TestRunnerService` | Orchestrates batch parsing with progress events |
| `PdfExportService` | Generates PDF reports using QuestPDF |
| `TestSummary` | Aggregated results (totals, averages, extremes) |
| `TestResult` | Per-file parse result record |

## Dependencies

- **WPF-UI** 4.3.0 — Modern WPF theming
- **QuestPDF** 2026.7.1 — PDF report generation
- **Serilog** — Structured logging
- **VideoGameFileSystemParser** — CHD file system parsing library (via SimpleChdDrive.Core)

## License

GNU General Public License v3.0 — see the [LICENSE](https://github.com/drpetersonfernandes/SimpleChdDrive/blob/main/LICENSE) file for details.
