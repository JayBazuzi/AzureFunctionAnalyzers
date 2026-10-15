---
name: route-constraints
description: How HTTP trigger route constraints (`{id:int}`) behave in Azure Functions and what analyzer rules about them should and shouldn't flag. Use when working on rules that inspect `HttpTrigger` routes or route parameters.
---

## Purpose

Route constraints are for disambiguating routes, not for input validation; invalid input should produce a `400`, not a `404`. See [Routing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing).


## Runtime behavior

Observed with the isolated worker (Worker 2.52, Core Tools 4.14), identical for the ASP.NET Core integration and the classic `HttpRequestData` model:

- A value that fails the constraint: route doesn't match, `404`.
- A value that passes the constraint but can't convert to the parameter type: `FunctionInputConverterException`, `500` with an empty body. Nullable parameters fail the same way.
- Conversion uses System.Text.Json, not ASP.NET type converters. So `{id:bool}` accepts `True` but only lowercase `true`/`false` bind, and `NaN`/`Infinity` don't bind to `double`.
- `Guid` and `DateTime` bind from their usual string forms.
- A `string` parameter receives the raw text (`007` stays `"007"`).
