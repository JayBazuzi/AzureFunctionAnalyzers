using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AzureFunctionAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RouteConstraintParameterTypeAnalyzer : DiagnosticAnalyzer
{
    private const string FunctionAttributeFullName = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
    private const string HttpTriggerAttributeFullName = "Microsoft.Azure.Functions.Worker.HttpTriggerAttribute";

    public static readonly DiagnosticDescriptor IncompatibleRouteConstraint = new(
        id: "AZURE_FUNCTIONS_0005",
        title: "Route constraint is incompatible with the parameter type",
        messageFormat: "Route parameter '{0}' has constraint '{1}', which no '{2}' value can satisfy",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every request that matches the route constraint will fail to bind to the parameter.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [IncompatibleRouteConstraint];

    private enum ValueKind { Integer, Real, Boolean, Guid, DateTime }

    private static readonly ImmutableArray<ValueKind> Numeric = [ValueKind.Integer, ValueKind.Real];

    // For each constraint, the kinds of parameter type that at least one matching value can bind to.
    // `alpha` is conservative: "true" binds to bool, "NaN" to double, month names may parse as dates.
    private static readonly Dictionary<string, ImmutableArray<ValueKind>> CompatibleKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["int"] = Numeric,
        ["long"] = Numeric,
        ["min"] = Numeric,
        ["max"] = Numeric,
        ["range"] = Numeric,
        ["decimal"] = Numeric,
        ["double"] = Numeric,
        ["float"] = Numeric,
        ["bool"] = [ValueKind.Boolean],
        ["guid"] = [ValueKind.Guid],
        ["datetime"] = [ValueKind.DateTime],
        ["alpha"] = [ValueKind.Boolean, ValueKind.DateTime],
    };

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var functionAttributeSymbol = context.Compilation.GetTypeByMetadataName(FunctionAttributeFullName);
        var httpTriggerAttributeSymbol = context.Compilation.GetTypeByMetadataName(HttpTriggerAttributeFullName);
        if (functionAttributeSymbol is null || httpTriggerAttributeSymbol is null)
        {
            return;
        }

        context.RegisterSymbolAction(
            c => AnalyzeMethod(c, functionAttributeSymbol, httpTriggerAttributeSymbol),
            SymbolKind.Method);
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol functionAttributeSymbol,
        INamedTypeSymbol httpTriggerAttributeSymbol)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (!method.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, functionAttributeSymbol)))
        {
            return;
        }

        var route = method.Parameters
            .SelectMany(p => p.GetAttributes())
            .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, httpTriggerAttributeSymbol))
            .SelectMany(a => a.NamedArguments)
            .Where(n => n.Key == "Route")
            .Select(n => n.Value.Value as string)
            .FirstOrDefault();
        if (route is null)
        {
            return;
        }

        foreach (var (name, constraints) in ParseRouteParameters(route))
        {
            var parameter = method.Parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (parameter is null || GetValueKind(parameter.Type) is not { } kind)
            {
                continue;
            }

            var incompatible = constraints.FirstOrDefault(c =>
                CompatibleKinds.TryGetValue(ConstraintName(c), out var kinds) && !kinds.Contains(kind));
            if (incompatible is null)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                IncompatibleRouteConstraint,
                parameter.Locations.FirstOrDefault() ?? Location.None,
                parameter.Name,
                incompatible,
                parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static ValueKind? GetValueKind(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            type = nullable.TypeArguments[0];
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
                return ValueKind.Integer;
            case SpecialType.System_Decimal:
            case SpecialType.System_Double:
            case SpecialType.System_Single:
                return ValueKind.Real;
            case SpecialType.System_Boolean:
                return ValueKind.Boolean;
            case SpecialType.System_DateTime:
                return ValueKind.DateTime;
        }

        return type.ToDisplayString() switch
        {
            "System.Guid" => ValueKind.Guid,
            "System.DateTimeOffset" => ValueKind.DateTime,
            _ => null,
        };
    }

    private static string ConstraintName(string constraint)
    {
        var parenthesis = constraint.IndexOf('(');
        return parenthesis < 0 ? constraint : constraint.Substring(0, parenthesis);
    }

    // Parses "users/{id:int:min(1)}/{*rest}" into ("id", ["int", "min(1)"]), ("rest", []).
    // "{{" and "}}" are literal braces, both in the template and inside constraint arguments.
    private static IEnumerable<(string Name, ImmutableArray<string> Constraints)> ParseRouteParameters(string route)
    {
        var index = 0;
        while (index < route.Length)
        {
            if (route[index] != '{')
            {
                index++;
                continue;
            }

            if (index + 1 < route.Length && route[index + 1] == '{')
            {
                index += 2;
                continue;
            }

            var end = index + 1;
            while (end < route.Length && !(route[end] == '}' && (end + 1 >= route.Length || route[end + 1] != '}')))
            {
                end += route[end] is '{' or '}' ? 2 : 1;
            }

            if (end >= route.Length)
            {
                yield break;
            }

            yield return ParseParameter(route.Substring(index + 1, end - index - 1));
            index = end + 1;
        }
    }

    private static (string Name, ImmutableArray<string> Constraints) ParseParameter(string text)
    {
        text = text.TrimStart('*');
        var nameEnd = 0;
        while (nameEnd < text.Length && text[nameEnd] is not (':' or '=' or '?'))
        {
            nameEnd++;
        }

        var name = text.Substring(0, nameEnd);
        var constraints = ImmutableArray.CreateBuilder<string>();
        var position = nameEnd;
        while (position < text.Length && text[position] == ':')
        {
            var start = ++position;
            var depth = 0;
            while (position < text.Length)
            {
                var c = text[position];
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                }
                else if (depth == 0 && c is ':' or '=' or '?')
                {
                    break;
                }

                position++;
            }

            constraints.Add(text.Substring(start, position - start));
        }

        return (name, constraints.ToImmutable());
    }
}
