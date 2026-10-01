using Microsoft.CodeAnalysis;

namespace AzureFunctionAnalyzers;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor FunctionClassNameMismatch = new(
        id: "AZURE_FUNCTIONS_0001",
        title: "Function class name should match the function name",
        messageFormat: "Class '{0}' contains [Function(\"{1}\")] and should be named '{2}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A class containing an Azure Function trigger method should be named after the function, e.g. [Function(\"X\")] -> class XFunction.");
}
