# AzureFunctionAnalyzers

[![NuGet](https://img.shields.io/nuget/v/AzureFunctionAnalyzers.svg)](https://www.nuget.org/packages/AzureFunctionAnalyzers)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AzureFunctionAnalyzers.svg)](https://www.nuget.org/packages/AzureFunctionAnalyzers)
[![Build and Test](https://github.com/JayBazuzi/AzureFunctionAnalyzers/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/JayBazuzi/AzureFunctionAnalyzers/actions/workflows/build-and-test.yml)

Roslyn analyzers for common Azure Functions patterns.

## Rules

<!-- include: test\AzureFunctionAnalyzers.Tests\RulesTableTests.RulesTable.verified.md -->
| Id | Title |
| --- | --- |
| AZURE_FUNCTIONS_0001 | Function class name should match the function name |
| AZURE_FUNCTIONS_0002 | Prefer ILogger<T> over ILogger |
| AZURE_FUNCTIONS_0003 | Prefer injecting ILogger<T> over FunctionContext.GetLogger |
<!-- endInclude -->
