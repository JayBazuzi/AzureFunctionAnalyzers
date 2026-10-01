# TODO

## Next rules



### HTTP method/route -> function name.

`[Function("GetUsers")]` on an HTTP-triggered function should match the trigger's HTTP method, e.g. HTTP method `GET` `/users` -> `GetUsers`. Needs a code fix for both the attribute argument and the class-name rule to stay in sync.

### Binding/attribute correctness.

Flag missing or mismatched trigger/binding attributes (e.g. parameter type doesn't match the binding, `[Function]` present but no trigger parameter, duplicate triggers on one method).

### No blocking calls

No blocking calls (`.Result`, `.Wait()`) in function methods.

### Prefer typed logger

`ILogger<T>` injected rather than `ILogger`.

## Project hygiene

- Update `README.md`'s rule table — AFA0001 was renamed to `AZURE_FUNCTIONS_0001`; keep the table in sync as rules are added/renamed.

