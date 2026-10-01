using System.Reflection;
using Microsoft.CodeAnalysis;

namespace AzureFunctionAnalyzers.Tests;

public class RulesTableTests
{
    [Fact]
    public Task RulesTable()
    {
        var descriptors =
            from fieldInfo in typeof(DiagnosticDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static)
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
