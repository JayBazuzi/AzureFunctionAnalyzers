using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AzureFunctionAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OneFunctionPerClassAnalyzer : DiagnosticAnalyzer
{
    private const string FunctionAttributeFullName = "Microsoft.Azure.Functions.Worker.FunctionAttribute";

    public static readonly DiagnosticDescriptor MultipleFunctionsInClass = new(
        id: "AZURE_FUNCTIONS_0004",
        title: "Class should contain only one Azure Function",
        messageFormat: "Class '{0}' contains {1} Azure Functions; each class should contain only one",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each class should contain a single Azure Function trigger method.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [MultipleFunctionsInClass];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var functionAttributeSymbol = context.Compilation.GetTypeByMetadataName(FunctionAttributeFullName);
        if (functionAttributeSymbol is null)
        {
            return;
        }

        context.RegisterSymbolAction(
            c => AnalyzeType(c, functionAttributeSymbol),
            SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol functionAttributeSymbol)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var functionCount = type.GetMembers().OfType<IMethodSymbol>().Count(m => m.GetAttributes()
            .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, functionAttributeSymbol)));
        if (functionCount < 2)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            MultipleFunctionsInClass,
            type.Locations.FirstOrDefault() ?? Location.None,
            type.Name,
            functionCount));
    }
}
