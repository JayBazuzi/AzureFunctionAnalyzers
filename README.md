# AzureFunctionAnalyzers

Roslyn analyzers for common Azure Functions patterns.

## Rules

<!-- include: test\AzureFunctionAnalyzers.Tests\RulesTableTests.RulesTable.verified.md -->
| Id | Title |
| --- | --- |
| AZURE_FUNCTIONS_0001 | Function class name should match the function name |
| AZURE_FUNCTIONS_0002 | Prefer ILogger<T> over ILogger |
| AZURE_FUNCTIONS_0003 | Prefer injecting ILogger<T> over FunctionContext.GetLogger |
<!-- endInclude -->
