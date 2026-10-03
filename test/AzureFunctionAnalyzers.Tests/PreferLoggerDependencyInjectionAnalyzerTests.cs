using Verify = AzureFunctionAnalyzers.Tests.CSharpAnalyzerVerifier<AzureFunctionAnalyzers.PreferLoggerDependencyInjectionAnalyzer>;

namespace AzureFunctionAnalyzers.Tests;

public class PreferLoggerDependencyInjectionAnalyzerTests
{
    [Fact]
    public async Task InjectedLogger_NoDiagnostic()
    {
        const string source = """
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Extensions.Logging;

            class GetUsersFunction
            {
                private readonly ILogger<GetUsersFunction> _logger;

                public GetUsersFunction(ILogger<GetUsersFunction> logger)
                {
                    _logger = logger;
                }

                [Function("GetUsers")]
                public void Run() { }
            }
            """;

        await Verify.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GetLoggerByCategoryName_ReportsDiagnostic()
    {
        const string source = """
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Extensions.Logging;

            class FunctionContext
            {
                public ILogger GetLogger(string categoryName) => null!;
            }

            class GetUsersFunction
            {
                [Function("GetUsers")]
                public void Run(FunctionContext context)
                {
                    var logger = {|#0:context.GetLogger("GetUsersFunction")|};
                }
            }
            """;

        var expected = Verify.Diagnostic("AZURE_FUNCTIONS_0003")
            .WithLocation(0)
            .WithArguments("GetUsersFunction", "GetLogger");

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task GetLoggerGeneric_ReportsDiagnostic()
    {
        const string source = """
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Extensions.Logging;

            class FunctionContext
            {
                public ILogger<T> GetLogger<T>() => null!;
            }

            // begin-snippet: PreferLoggerDependencyInjectionAnalyzerExample
            class GetUsersFunction
            {
                [Function("GetUsers")]
                public void Run(FunctionContext context)
                {
                    var logger = {|#0:context.GetLogger<GetUsersFunction>()|};  // warning: Use constructor-injected ILogger<GetUsersFunction> instead of calling 'GetLogger'
                }
            }
            // end-snippet
            """;

        var expected = Verify.Diagnostic("AZURE_FUNCTIONS_0003")
            .WithLocation(0)
            .WithArguments("GetUsersFunction", "GetLogger<GetUsersFunction>");

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task GetLoggerByCategoryName_NoFunctionMethod_NoDiagnostic()
    {
        const string source = """
            using Microsoft.Extensions.Logging;

            class FunctionContext
            {
                public ILogger GetLogger(string categoryName) => null!;
            }

            class NotAFunction
            {
                public void Run(FunctionContext context)
                {
                    var logger = context.GetLogger("NotAFunction");
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(source);
    }
}
