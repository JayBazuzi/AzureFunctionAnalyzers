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
<a id='snippet-FunctionClassNameAnalyzerExample'></a>
```cs
class UserGetter
{
    [{|#0:Function("GetUsers")|}] // warning: Class name 'UserGetter' does not match function name 'GetUsers', expected 'GetUsersFunction'
    public void Run() { }
}
```
<sup><a href='/test/AzureFunctionAnalyzers.Tests/FunctionClassNameAnalyzerTests.cs#L30-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-FunctionClassNameAnalyzerExample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: PreferTypedLoggerAnalyzerExample -->
<a id='snippet-PreferTypedLoggerAnalyzerExample'></a>
```cs
var logger = {|#0:context.GetLogger("GetUsersFunction")|};  // warning: 'logger' should be typed 'ILogger<GetUsersFunction>' instead of 'ILogger'
```
<sup><a href='/test/AzureFunctionAnalyzers.Tests/PreferTypedLoggerAnalyzerTests.cs#L76-L78' title='Snippet source file'>snippet source</a> | <a href='#snippet-PreferTypedLoggerAnalyzerExample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: PreferLoggerDependencyInjectionAnalyzerExample -->
<a id='snippet-PreferLoggerDependencyInjectionAnalyzerExample'></a>
```cs
class GetUsersFunction
{
    [Function("GetUsers")]
    public void Run(FunctionContext context)
    {
        var logger = {|#0:context.GetLogger<GetUsersFunction>()|};  // warning: Use constructor-injected ILogger<GetUsersFunction> instead of calling 'GetLogger'
    }
}
```
<sup><a href='/test/AzureFunctionAnalyzers.Tests/PreferLoggerDependencyInjectionAnalyzerTests.cs#L72-L81' title='Snippet source file'>snippet source</a> | <a href='#snippet-PreferLoggerDependencyInjectionAnalyzerExample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
