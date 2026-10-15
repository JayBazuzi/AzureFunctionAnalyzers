# TODO

## How to pick up a rule

- Each rule below says whether it's ready to build or needs research first. Do the research before writing code; several earlier ideas turned out to be false positives once the runtime was checked.
- Rules target the isolated worker model (`Microsoft.Azure.Functions.Worker.*`). Look up attribute types by metadata name and bail out if they're missing, as in [RouteConstraintParameterTypeAnalyzer.cs](src/AzureFunctionAnalyzers/RouteConstraintParameterTypeAnalyzer.cs).
- Rule IDs are `AZURE_FUNCTIONS_NNNN`; the next free one is `AZURE_FUNCTIONS_0006`. Add tests in `test/AzureFunctionAnalyzers.Tests/`, a README example via `begin-snippet`, and let `RulesTableTests` regenerate the rules table.
- Before writing an HTTP route rule, read the `route-constraints` skill. Record any new runtime findings there, with the Worker and Core Tools versions you tested against.
- Don't duplicate rules that already exist. The Worker SDK ships [AZFW0001–AZFW0017](https://github.com/Azure/azure-functions-dotnet-worker/blob/main/docs/analyzer-rules/index.md) (WebJobs attributes in isolated projects, `async void`, invalid binding types, missing `HttpResult`, duplicate function names, and more). `Microsoft.DurableTask.Analyzers` covers orchestrator determinism.

## Ready to build

### Invalid function name

Flag a `[Function("...")]` name that the host will reject.

Evidence: the host validates names with `^[a-z][a-z0-9_\-]{0,127}$(?<!^host$)`, case-insensitive (`IsValidFunctionName` in `src/WebJobs.Script/Utility.cs` in [azure-functions-host](https://github.com/Azure/azure-functions-host)). So a name must:

- start with a letter,
- contain only letters, digits, `_`, and `-`,
- be at most 128 characters,
- not be `host` (any case).

Failure mode: the host doesn't fail to start. `WorkerFunctionMetadataValidator` (`src/WebJobs.Script.Grpc/Metadata/WorkerFunctionMetadataValidator.cs`) drops the invalid function, logs an error, and keeps the rest, so the function silently doesn't exist. Nothing in the Worker SDK's analyzers or its source generator (`sdk/Sdk.Generators/FunctionMetadataProviderGenerator`) checks this.

Notes:

- Only check when the name is a compile-time constant (`nameof(...)` and `const` strings are fine; skip anything else).
- Report on the attribute argument. Say which rule was broken (bad first character, bad character, too long, reserved).
- Severity: Error is justified, since the function will never run.

### `ILogger<T>` type argument doesn't match the containing class

Flag `ILogger<T>` where `T` isn't the class that uses it, e.g. `ILogger<GetUsersFunction>` injected into `DeleteUserFunction`. It's usually a copy-paste mistake.

Evidence: this is a convention, not a runtime failure. [Logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) says the category "convention is to use the fully qualified class name", and `ILogger<T>` is equivalent to `CreateLogger` with `T`'s full name. A mismatch puts logs under the wrong category, which breaks log filtering and makes logs misleading.

Existing analyzers: there's no CA rule. Small third-party packages cover constructor parameters only: [Eliseev.LoggerAnalyzer](https://www.nuget.org/packages/Eliseev.LoggerAnalyzer), [IncorrectLoggingAnalyzer](https://www.nuget.org/packages/IncorrectLoggingAnalyzer), and [MicrosoftLogger.Analyzer](https://www.nuget.org/packages/microsoftlogger.analyzer). It still belongs here next to `AZURE_FUNCTIONS_0002` and `AZURE_FUNCTIONS_0003`, and it should also cover `FunctionContext.GetLogger<T>()`.

Notes:

- Check constructor parameters (including primary constructors), fields and properties of type `ILogger<T>`, and `GetLogger<T>()` calls.
- Decide how to handle nested types and partial classes. The reasonable default is that `T` must be the containing type; allowing the outer type for a nested class is a judgment call worth a test.
- Only apply it to classes that contain a `[Function]` method, or to all classes? Scoping it to function classes keeps it in this package's lane; say which you chose in the rule description.
- A code fix that changes `T` to the containing type is straightforward.

## Needs research first

### Duplicate and overlapping HTTP routes

Flag two HTTP-triggered functions whose routes and methods can match the same request.

What's known from the source: the host doesn't use endpoint routing, so there's no "ambiguous match" error. `WebScriptHostHttpRoutesManager.InitializeHttpFunctionRoutes` (azure-functions-host) maps each function through `WebJobsRouteBuilder` (`src/WebJobs.Extensions.Http/Routing/WebJobsRouteBuilder.cs` in [azure-webjobs-sdk-extensions](https://github.com/Azure/azure-webjobs-sdk-extensions)), which puts them in a plain `RouteCollection`. `RouteCollection` tries routes in registration order and the first match wins. So:

- Two identical routes with overlapping methods: the second function never runs, with no error.
- Overlapping routes: if `users/{id}` is registered before `users/me`, then `users/me` is unreachable, because nothing sorts routes by specificity.
- A function with no `Route` uses its function name as the route, so include those.

Research needed, against a real app with Core Tools (the same way the `route-constraints` skill findings were gathered):

1. What order are functions registered in? It might be source order, alphabetical, or metadata-generator order. If it's predictable, the rule can say which function wins.
2. Confirm identical routes: same route, same method, two functions. Which one runs? Is anything logged at startup?
3. Confirm overlap: `users/{id}` and `users/me` in both orders. Does `users/me` ever reach its function?
4. Do route constraints separate routes as expected? For example, `items/{id:int}` and `items/{name}` should coexist when `int` is checked first.
5. Does a function with no methods listed match every method (it should, since the method constraint is skipped when `Methods` is null)?

Design notes:

- Start with exact duplicates after normalizing parameter names (`users/{id}` equals `users/{userId}`), case-insensitive, with overlapping methods. Add overlap detection only if research confirms first-match-wins.
- This needs a compilation-wide analysis (`RegisterCompilationStartAction` collecting routes, then `RegisterCompilationEndAction`). Note that compilation-end diagnostics don't show live in the IDE by default.
- `ParseRouteParameters` in `RouteConstraintParameterTypeAnalyzer` already parses route templates; reuse it.
- Can't see `routePrefix` in host.json, but the prefix is the same for all functions, so it doesn't affect whether two routes collide.

### Route starting with `/`

Flag `Route = "/users"`.

What's known from the source: `WebJobsRouteBuilder.BuildRouteTemplate` joins with `routePrefix + '/' + routeTemplate` and I found no trimming in the host, HTTP extension, or worker. With the default prefix `api`, `/users` becomes `api//users`. ASP.NET Core route templates reject consecutive `/` separators, so this probably throws when the host builds routes.

Research needed:

1. Run a function with `Route = "/users"` under the default prefix. Does the host fail to start, skip that route, or work anyway?
2. Check whether the Worker SDK's metadata generator or the host trims the leading slash somewhere I missed (search for `TrimStart` near route handling in all three repos).
3. With `routePrefix: ""` the template is just `/users`, which ASP.NET Core accepts. If the rule fires anyway, the message should still recommend removing the slash, since it's never needed.

Don't flag a route starting with `api/`. People who set `routePrefix` to `""` sometimes add `api/` back on purpose, and an analyzer can't see host.json unless it's added as an `AdditionalFiles` item.

### Unbindable value-type parameter

Originally proposed as "method parameter with no binding attribute that isn't a route token". That version is wrong, because unattributed parameters legitimately bind from the query string.

What's known from the source:

- The worker binds an unattributed parameter by looking its name up in input data, then trigger metadata (`DefaultFunctionInputBindingFeature` in [azure-functions-dotnet-worker](https://github.com/Azure/azure-functions-dotnet-worker)). The lookup ignores case (`GrpcFunctionBindingsFeature` builds the dictionaries with `StringComparer.OrdinalIgnoreCase`).
- For HTTP triggers, trigger metadata includes route values and every query-string value (`HttpTriggerAttributeBindingProvider` in azure-webjobs-sdk-extensions), plus `Query`, `Headers`, and the request itself.
- If no value is found: a reference or nullable type gets `null`, a parameter with a default value gets the default, and a non-nullable value type with no default throws `FunctionInputConverterException` ("Could not populate the value for '...' parameter").

So the actual bug is narrow: a non-nullable value-type parameter with no default, no binding attribute, and no matching route token, e.g. `int page`. When the caller leaves out `?page=`, they get a 500 instead of a 400. A misspelled route token on an `int` parameter falls into the same case. This matches the "invalid input should be a 400, not a 404 or 500" reasoning in the `route-constraints` skill.

Research needed:

1. Confirm a missing `?page=` gives a 500 with an empty body, under both the classic `HttpRequestData` model and ASP.NET Core integration.
2. Confirm `int page = 1` and `int? page` both avoid the failure.
3. List which parameter types are injected without a binding attribute and must be excluded: at least `FunctionContext`, `CancellationToken`, the trigger's own parameter, and anything with a binding attribute. Check what else the worker injects.
4. Decide whether this is limited to HTTP triggers. Other triggers have their own trigger metadata (e.g. queue message properties), so the same failure may apply but with different valid names.

## Other ideas

### HTTP method/route -> function name.

`[Function("GetUsers")]` on an HTTP-triggered function should match the trigger's HTTP method, e.g. HTTP method `GET` `/users` -> `GetUsers`. Needs a code fix for both the attribute argument and the class-name rule to stay in sync.

### Binding/attribute correctness.

Flag missing or mismatched trigger/binding attributes (e.g. parameter type doesn't match the binding, `[Function]` present but no trigger parameter, duplicate triggers on one method). Check the AZFW rules first; AZFW0001, AZFW0005, AZFW0008, AZFW0010, and AZFW0011 already cover parts of this.

### No blocking calls

No blocking calls (`.Result`, `.Wait()`) in function methods.

### Route under a reserved host path

The host returns 404 for any request under `/admin` or `/runtime` and logs an error when it matches a function route (`ReservedRouteGuardMiddleware` and `HttpRequestExtensions.IsReservedRouteRequest` in azure-functions-host). This only matters when `routePrefix` is `""`, so the analyzer would need host.json as an `AdditionalFiles` item. Worth doing only if a host.json-aware rule gets built for some other reason.

## Rejected ideas

- Duplicate function names: already covered by [AZFW0017](https://github.com/Azure/azure-functions-dotnet-worker/blob/main/docs/analyzer-rules/AZFW0017.md) (Error, case-insensitive) from the Worker SDK's source generator.
