# AzureFunctionAnalyzers

Roslyn analyzers for common Azure Functions patterns.

## Rules

- Function class name should match the function name<!-- include: test\AzureFunctionAnalyzers.Tests\RulesTableTests.RulesList.verified.md -->
- Prefer `ILogger<T>` over `ILogger`
- Prefer injecting `ILogger<T>` over `FunctionContext.GetLogger()`
- Class should contain only one Azure Function
- Route constraint is incompatible with the parameter type<!-- endInclude -->

## Example

For example, this class's name doesn't match its function name:

<!-- include: test\AzureFunctionAnalyzers.Tests\NugetReadmeTests.Example.verified.md -->
```cs
class UserGetter
{
    [Function("GetUsers")] // warning: Class name 'UserGetter' does not match function name 'GetUsers', expected 'GetUsersFunction'
    public void Run()
    {
        //...
    }
}
```
<!-- endInclude -->

For more details, see the [full README](https://github.com/JayBazuzi/AzureFunctionAnalyzers#readme).
