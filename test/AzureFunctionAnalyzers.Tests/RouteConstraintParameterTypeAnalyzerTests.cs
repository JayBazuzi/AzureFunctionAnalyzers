using Verify = AzureFunctionAnalyzers.Tests.CSharpAnalyzerVerifier<AzureFunctionAnalyzers.RouteConstraintParameterTypeAnalyzer>;

namespace AzureFunctionAnalyzers.Tests;

public class RouteConstraintParameterTypeAnalyzerTests
{
    private static string Function(string route, string parameter) => $$"""
        using System;
        using Microsoft.Azure.Functions.Worker;

        class GetUserFunction
        {
            [Function("GetUser")]
            public void Run(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{{route}}")] object request,
                {{parameter}})
            {
            }
        }
        """;

    [Theory]
    [InlineData("users/{id:int}", "int id")]
    [InlineData("users/{id:int}", "long id")]
    [InlineData("users/{id:int}", "string id")]
    [InlineData("users/{id:int}", "int? id")]
    [InlineData("users/{id:long}", "int id")]
    [InlineData("users/{id:guid}", "Guid id")]
    [InlineData("users/{id:bool}", "bool id")]
    [InlineData("users/{id:datetime}", "DateTime id")]
    [InlineData("users/{id:datetime}", "DateTimeOffset id")]
    [InlineData("users/{id:decimal}", "int id")]
    [InlineData("users/{id:alpha}", "string id")]
    [InlineData("users/{id:alpha}", "bool id")]
    [InlineData("users/{id:length(3)}", "int id")]
    [InlineData("users/{id:regex(^\\\\d{{3}}$)}", "int id")]
    [InlineData("users/{id}", "Guid id")]
    [InlineData("users/{name:int}", "Guid id")]
    [InlineData("users/{id:int}", "System.IO.Stream id")]
    public async Task CompatibleOrUnknown_NoDiagnostic(string route, string parameter)
    {
        await Verify.VerifyAnalyzerAsync(Function(route, parameter));
    }

    [Theory]
    [InlineData("users/{id:alpha}", "int id", "alpha", "int")]
    [InlineData("users/{id:alpha}", "Guid id", "alpha", "Guid")]
    [InlineData("users/{id:guid}", "int id", "guid", "int")]
    [InlineData("users/{id:int}", "Guid id", "int", "Guid")]
    [InlineData("users/{id:int}", "bool id", "int", "bool")]
    [InlineData("users/{id:bool}", "int id", "bool", "int")]
    [InlineData("users/{id:datetime}", "int id", "datetime", "int")]
    [InlineData("users/{id:min(1)}", "Guid id", "min(1)", "Guid")]
    [InlineData("users/{id:int?}", "Guid id", "int", "Guid")]
    [InlineData("users/{id:int}", "Guid? id", "int", "Guid?")]
    [InlineData("users/{id:int=5}", "Guid id", "int", "Guid")]
    [InlineData("users/{ID:int}", "Guid id", "int", "Guid")]
    [InlineData("users/{id:minlength(2):alpha}", "int id", "alpha", "int")]
    public async Task Incompatible_ReportsDiagnostic(string route, string parameter, string constraint, string type)
    {
        var source = Function(route, parameter.Replace(" id", " {|#0:id|}"));

        var expected = Verify.Diagnostic("AZURE_FUNCTIONS_0005")
            .WithLocation(0)
            .WithArguments("id", constraint, type);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NotAFunction_NoDiagnostic()
    {
        const string source = """
            using System;
            using Microsoft.Azure.Functions.Worker;

            class GetUserFunction
            {
                public void Run(
                    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id:alpha}")] object request,
                    int id)
                {
                }
            }
            """;

        await Verify.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Example()
    {
        const string source = """
            using Microsoft.Azure.Functions.Worker;

            class GetUserFunction
            {
                // begin-snippet: RouteConstraintParameterTypeAnalyzerExample
                [Function("GetUser")]
                public void Run(
                    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id:alpha}")] object request,
                    int {|#0:id|}) // warning: Route parameter 'id' has constraint 'alpha', which no 'int' value can satisfy
                {
                    //...
                // end-snippet
                }
            }
            """;

        var expected = Verify.Diagnostic("AZURE_FUNCTIONS_0005")
            .WithLocation(0)
            .WithArguments("id", "alpha", "int");

        await Verify.VerifyAnalyzerAsync(source, expected);
    }
}
