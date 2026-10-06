using Verify = AzureFunctionAnalyzers.Tests.CSharpAnalyzerVerifier<AzureFunctionAnalyzers.OneFunctionPerClassAnalyzer>;

namespace AzureFunctionAnalyzers.Tests;

public class OneFunctionPerClassAnalyzerTests
{
    [Fact]
    public async Task OneFunction_NoDiagnostic()
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
    public async Task TwoFunctions_ReportsDiagnostic()
    {
        const string source = """
            using Microsoft.Azure.Functions.Worker;

            class {|#0:Users|}
            {
                [Function("GetUsers")]
                public void Get() { }

                [Function("PutUsers")]
                public void Put() { }
            }
            """;

        var expected = Verify.Diagnostic("AZURE_FUNCTIONS_0004")
            .WithLocation(0)
            .WithArguments("Users", 2);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }
}
