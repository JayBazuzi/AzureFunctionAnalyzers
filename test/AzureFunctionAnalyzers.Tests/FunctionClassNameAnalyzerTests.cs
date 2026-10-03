using Verify = AzureFunctionAnalyzers.Tests.CSharpAnalyzerVerifier<AzureFunctionAnalyzers.FunctionClassNameAnalyzer>;
using VerifyFix = AzureFunctionAnalyzers.Tests.CSharpCodeFixVerifier<AzureFunctionAnalyzers.FunctionClassNameAnalyzer, AzureFunctionAnalyzers.FunctionClassNameCodeFixProvider>;

namespace AzureFunctionAnalyzers.Tests;

public class FunctionClassNameAnalyzerTests
{
    [Fact]
    public async Task MatchingClassName_NoDiagnostic()
    {
        const string source = """
            using Microsoft.Azure.Functions.Worker;

            class GetUsersFunction
            {
                [Function("GetUsers")]
                public void Run() { }
            }
            """;

        await Verify.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MismatchedClassName_ReportsDiagnostic()
    {
        const string source = """
            using Microsoft.Azure.Functions.Worker;

            // begin-snippet: FunctionClassNameAnalyzerExample
            class UserGetter
            {
                [{|#0:Function("GetUsers")|}] // warning: Class name 'UserGetter' does not match function name 'GetUsers', expected 'GetUsersFunction'
                public void Run()
                {
                    //...
                }
            }
            // end-snippet
            """;

        var expected = Verify.Diagnostic("AZURE_FUNCTIONS_0001")
            .WithLocation(0)
            .WithArguments("UserGetter", "GetUsers", "GetUsersFunction");

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task MismatchedClassName_CodeFixRenamesClass()
    {
        const string source = """
            using Microsoft.Azure.Functions.Worker;

            class UserGetter
            {
                [{|#0:Function("GetUsers")|}]
                public void Run() { }
            }
            """;

        const string fixedSource = """
            using Microsoft.Azure.Functions.Worker;

            class GetUsersFunction
            {
                [Function("GetUsers")]
                public void Run() { }
            }
            """;

        var expected = VerifyFix.Diagnostic("AZURE_FUNCTIONS_0001")
            .WithLocation(0)
            .WithArguments("UserGetter", "GetUsers", "GetUsersFunction");

        await VerifyFix.VerifyCodeFixAsync(source, expected, fixedSource);
    }
}
