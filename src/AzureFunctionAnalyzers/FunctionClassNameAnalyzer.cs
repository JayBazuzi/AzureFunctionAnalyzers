using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AzureFunctionAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FunctionClassNameAnalyzer : DiagnosticAnalyzer
{
    private const string FunctionAttributeFullName = "Microsoft.Azure.Functions.Worker.FunctionAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [DiagnosticDescriptors.FunctionClassNameMismatch];

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

        context.RegisterSyntaxNodeAction(
            c => AnalyzeMethod(c, functionAttributeSymbol),
            SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, INamedTypeSymbol functionAttributeSymbol)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);
        if (methodSymbol is null)
        {
            return;
        }

        var functionAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, functionAttributeSymbol));
        if (functionAttribute is null || functionAttribute.ConstructorArguments.Length == 0)
        {
            return;
        }

        var functionName = functionAttribute.ConstructorArguments[0].Value as string;
        if (string.IsNullOrEmpty(functionName))
        {
            return;
        }

        var containingType = methodSymbol.ContainingType;
        var expectedClassName = functionName + "Function";
        if (containingType.Name == expectedClassName)
        {
            return;
        }

        var attributeSyntax = functionAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken);
        var location = attributeSyntax?.GetLocation()
            ?? containingType.Locations.FirstOrDefault()
            ?? Location.None;

        var properties = ImmutableDictionary.Create<string, string?>()
            .Add("ExpectedClassName", expectedClassName);

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.FunctionClassNameMismatch,
            location,
            properties,
            containingType.Name,
            functionName,
            expectedClassName));
    }
}
