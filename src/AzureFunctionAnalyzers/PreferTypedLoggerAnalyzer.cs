using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AzureFunctionAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferTypedLoggerAnalyzer : DiagnosticAnalyzer
{
    private const string FunctionAttributeFullName = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
    private const string LoggerFullName = "Microsoft.Extensions.Logging.ILogger";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [DiagnosticDescriptors.PreferTypedLogger];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var functionAttributeSymbol = context.Compilation.GetTypeByMetadataName(FunctionAttributeFullName);
        var loggerSymbol = context.Compilation.GetTypeByMetadataName(LoggerFullName);
        if (functionAttributeSymbol is null || loggerSymbol is null)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(
            c => AnalyzeConstructor(c, functionAttributeSymbol, loggerSymbol),
            SyntaxKind.ConstructorDeclaration);

        context.RegisterSyntaxNodeAction(
            c => AnalyzeGetLoggerInvocation(c, functionAttributeSymbol, loggerSymbol),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeConstructor(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol functionAttributeSymbol,
        INamedTypeSymbol loggerSymbol)
    {
        var constructorDeclaration = (ConstructorDeclarationSyntax)context.Node;
        var constructorSymbol = context.SemanticModel.GetDeclaredSymbol(constructorDeclaration, context.CancellationToken);
        if (constructorSymbol is null)
        {
            return;
        }

        var containingType = constructorSymbol.ContainingType;
        if (!HasFunctionMethod(containingType, functionAttributeSymbol))
        {
            return;
        }

        foreach (var parameter in constructorSymbol.Parameters)
        {
            if (!SymbolEqualityComparer.Default.Equals(parameter.Type, loggerSymbol))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreferTypedLogger,
                parameter.Locations.FirstOrDefault() ?? Location.None,
                parameter.Name,
                containingType.Name));
        }
    }

    private static void AnalyzeGetLoggerInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol functionAttributeSymbol,
        INamedTypeSymbol loggerSymbol)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.Name != "GetLogger"
            || methodSymbol.Parameters.Length != 1
            || methodSymbol.Parameters[0].Type.SpecialType != SpecialType.System_String
            || !SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, loggerSymbol))
        {
            return;
        }

        var typeDeclaration = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDeclaration is null
            || context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) is not INamedTypeSymbol containingType
            || !HasFunctionMethod(containingType, functionAttributeSymbol))
        {
            return;
        }

        var name = invocation.Ancestors().OfType<VariableDeclaratorSyntax>().FirstOrDefault()?.Identifier.Text
            ?? "logger";

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PreferTypedLogger,
            invocation.GetLocation(),
            name,
            containingType.Name));
    }

    private static bool HasFunctionMethod(INamedTypeSymbol containingType, INamedTypeSymbol functionAttributeSymbol) =>
        containingType.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(m => m.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, functionAttributeSymbol)));
}
