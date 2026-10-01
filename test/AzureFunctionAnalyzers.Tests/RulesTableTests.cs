using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AzureFunctionAnalyzers.Tests;

public class RulesTableTests
{
    [Fact]
    public Task RulesTable()
    {
        var descriptors =
            from type in typeof(FunctionClassNameAnalyzer).Assembly.GetTypes()
            where typeof(DiagnosticAnalyzer).IsAssignableFrom(type) && !type.IsAbstract
            from fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.Static)
            where fieldInfo.FieldType == typeof(DiagnosticDescriptor)
            let descriptor = (DiagnosticDescriptor)fieldInfo.GetValue(null)!
            orderby descriptor.Id
            select $"| {descriptor.Id} | {descriptor.Title} |";

        var result = $"""
        | Id | Title |
        | --- | --- |
        {string.Join("\n", descriptors)}
        """;

        return Verify(result, extension: "md");
    }
}
