using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AzureFunctionAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferLoggerDependencyInjectionAnalyzer : DiagnosticAnalyzer
{
    private const string FunctionAttributeFullName = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
    private const string FunctionContextTypeName = "FunctionContext";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [DiagnosticDescriptors.PreferLoggerDependencyInjection];

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
            c => AnalyzeGetLoggerInvocation(c, functionAttributeSymbol),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeGetLoggerInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol functionAttributeSymbol)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.Name != "GetLogger" || !IsFunctionContextGetLogger(methodSymbol))
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

        var invocationText = invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.ToString()
            : invocation.Expression.ToString();

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PreferLoggerDependencyInjection,
            invocation.GetLocation(),
            containingType.Name,
            invocationText));
    }

    private static bool IsFunctionContextGetLogger(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ContainingType?.Name == FunctionContextTypeName)
        {
            return true;
        }

        var reducedFrom = methodSymbol.ReducedFrom ?? (methodSymbol.IsExtensionMethod ? methodSymbol : null);
        var receiverType = reducedFrom?.Parameters.FirstOrDefault()?.Type;
        return receiverType?.Name == FunctionContextTypeName;
    }

    private static bool HasFunctionMethod(INamedTypeSymbol containingType, INamedTypeSymbol functionAttributeSymbol) =>
        containingType.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(m => m.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, functionAttributeSymbol)));
}
