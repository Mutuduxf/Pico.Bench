# Pico.Bench TUnit Test Suite

Comprehensive unit tests for the Pico.Bench formatter module using the TUnit testing framework.

## Overview

This test project provides 100% branch coverage for all formatter components in the Pico.Bench library:
- ConsoleFormatter
- CsvFormatter  
- HtmlFormatter
- MarkdownFormatter
- SummaryFormatter
- FormatterBase
- FormatterOptions

## Test Structure

- **Formatters/**: Unit tests for individual formatter classes
- **Integration/**: End-to-end tests for the full formatting pipeline
- **TestData/**: Factory classes for generating test data
- **Utilities/**: Helper classes for file system operations and test logging

## Requirements

- .NET 10.0
- TUnit 1.17.11+ (automatically resolved via package reference)

## Running Tests

```bash
cd tests
dotnet test
```

## Test Categories

Tests are categorized using TUnit properties:
- `Category=Formatter`: All formatter tests
- `SubCategory`: Specific formatter type (Console, CSV, HTML, Markdown, Summary)
- `FileSystem=true`: Tests that perform file system operations
- `Performance=true`: Performance-sensitive tests

## Coverage Goal

Aim for 100% branch coverage across all formatter classes with comprehensive edge case testing.