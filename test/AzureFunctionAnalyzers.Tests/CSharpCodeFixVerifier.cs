using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace AzureFunctionAnalyzers.Tests;

internal static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic()
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();

    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.AdditionalReferences.Add(typeof(Microsoft.Azure.Functions.Worker.FunctionAttribute).Assembly);
        test.FixedState.AdditionalReferences.Add(typeof(Microsoft.Azure.Functions.Worker.FunctionAttribute).Assembly);
        test.ExpectedDiagnostics.Add(expected);
        return test.RunAsync();
    }
}
