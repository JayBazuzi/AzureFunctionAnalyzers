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
| AZURE_FUNCTIONS_0002 | Prefer `ILogger<T>` over `ILogger` |
| AZURE_FUNCTIONS_0003 | Prefer injecting `ILogger<T>` over `FunctionContext.GetLogger()` |
<!-- endInclude -->

## Examples

<!-- snippet: FunctionClassNameAnalyzerExample -->
```cs
class UserGetter
{
    [{|#0:Function("GetUsers")|}] // warning: Class name 'UserGetter' does not match function name 'GetUsers', expected 'GetUsersFunction'
    public void Run()
    {
        //...
    }
}
```
<!-- endSnippet -->

<!-- snippet: PreferTypedLoggerAnalyzerExample -->
```cs
var logger = {|#0:context.GetLogger("GetUsersFunction")|};  // warning: 'logger' should be typed 'ILogger<GetUsersFunction>' instead of 'ILogger'
```
<!-- endSnippet -->

<!-- snippet: PreferLoggerDependencyInjectionAnalyzerExample -->
```cs
class GetUsersFunction
{
    [Function("GetUsers")]
    public void Run(FunctionContext context)
    {
        var logger = {|#0:context.GetLogger<GetUsersFunction>()|};  // warning: Use constructor-injected ILogger<GetUsersFunction> instead of calling 'GetLogger'
        //...
    }
}
```
<!-- endSnippet -->
