using Verify = AzureFunctionAnalyzers.Tests.CSharpAnalyzerVerifier<AzureFunctionAnalyzers.PreferTypedLoggerAnalyzer>;

namespace AzureFunctionAnalyzers.Tests;

public class PreferTypedLoggerAnalyzerTests
{
    [Fact]
    public async Task TypedLogger_NoDiagnostic()
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
    public async Task UntypedLogger_ReportsDiagnostic()
    {
        const string source = """
            using Microsoft.Azure.Functions.Worker;
            using Microsoft.Extensions.Logging;

            class GetUsersFunction
            {
                private readonly ILogger _logger;

                public GetUsersFunction(ILogger {|#0:logger|})
                {
                    _logger = logger;
                }

                [Function("GetUsers")]
                public void Run() { }
            }
            """;

        var expected = Verify.Diagnostic("AZURE_FUNCTIONS_0002")
            .WithLocation(0)
            .WithArguments("logger", "GetUsersFunction");

        await Verify.VerifyAnalyzerAsync(source, expected);
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

        var expected = Verify.Diagnostic("AZURE_FUNCTIONS_0002")
            .WithLocation(0)
            .WithArguments("logger", "GetUsersFunction");

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task UntypedLogger_NoFunctionMethod_NoDiagnostic()
    {
        const string source = """
            using Microsoft.Extensions.Logging;

            class NotAFunction
            {
                private readonly ILogger _logger;

                public NotAFunction(ILogger logger)
                {
                    _logger = logger;
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(source);
    }
}
