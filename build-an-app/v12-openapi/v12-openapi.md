# v12: OpenAPI

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v12-openapi)

The API is now large enough that a caller needs a contract: which paths exist, which bodies they take, which responses they return and how to authenticate. Version 12 generates an OpenAPI document from the endpoints and tests that the document matches the surface.

The finished solution is in `build-an-app/v12-openapi`.

## Write the test first

```csharp
[TestMethod]
public async Task SwaggerDocument_ListsTheApiSurface()
{
    using var api = new Api();

    var json = await api.Client.GetStringAsync("/swagger/v1/swagger.json");

    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;
    var paths = root.GetProperty("paths");

    Assert.IsTrue(paths.TryGetProperty("/tasks", out _));
    Assert.IsTrue(paths.TryGetProperty("/tasks/{id}", out _));
    Assert.IsTrue(paths.TryGetProperty("/tasks/{id}/done", out _));
    Assert.IsTrue(paths.TryGetProperty("/auth/token", out _));
    Assert.IsTrue(paths.TryGetProperty("/diagnostics", out _));

    var schemas = root.GetProperty("components").GetProperty("schemas");

    Assert.IsTrue(schemas.TryGetProperty("TaskItem", out _));
    Assert.IsTrue(schemas.TryGetProperty("CreateTaskRequest", out _));

    var security = root
        .GetProperty("components")
        .GetProperty("securitySchemes")
        .GetProperty("Bearer");

    Assert.AreEqual("http", security.GetProperty("type").GetString());
    Assert.AreEqual("bearer", security.GetProperty("scheme").GetString());
}
```

Against v11 the route returns 404. The test reads the document as JSON instead of trusting the UI.

## Add Swashbuckle

```sh
dotnet add src/TaskTracker.Api package Swashbuckle.AspNetCore --version 6.5.0
```

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskTracker API",
        Version = "v1",
        Description = "Task tracker used in the Learn .NET with Tests book."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by POST /auth/token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

and serve it:

```csharp
app.UseSwagger();
app.UseSwaggerUI();
```

Green. The document is at `/swagger/v1/swagger.json` and the UI at `/swagger`.

## How it works

Swashbuckle reads the endpoint metadata that ASP.NET Core built from the route handlers: the path, the HTTP method, the request body type and the response types. It writes an OpenAPI document with `paths`, `components/schemas` and `components/securitySchemes`.

The security definition and requirement are what make the UI show an Authorize button and let a caller paste the JWT. The document still lists the endpoints; the security scheme tells a client how to call them.

The test pins the contract. A route rename, a removed endpoint or a changed request type fails the test before a consumer discovers the break. That is a contract test, not a test of Swashbuckle's formatting.

## Refactor

A generated document is only useful if it stays accurate:

- Add XML documentation and examples when a field needs a human explanation. Enable `GenerateDocumentationFile` and include the file in `AddSwaggerGen`.
- Version the document when the API changes in a breaking way. `SwaggerDoc("v2", ...)` and a versioned route plan are cheaper than surprising clients.
- Hide internal endpoints. A diagnostics route with process ids is useful in development and should not be in a public document; use endpoint metadata or a separate document.
- Test the document in CI, as this chapter does. The UI is a convenience; the JSON is the contract.
- In .NET 9 and later, the built-in OpenAPI document generator replaces Swashbuckle's document generation; the book stays on Swashbuckle because the project targets .NET 6.

The document also exposes the auth flow: a client calls `/auth/token` without a token, then sends `Authorization: Bearer ...` to the task routes. The security scheme is documentation for that flow, not a replacement for the middleware.

## Wrapping up

What we have covered:

- `AddEndpointsApiExplorer` and `AddSwaggerGen` for minimal APIs.
- A documented API with paths, schemas and a Bearer security scheme.
- `UseSwagger` and the Swagger UI.
- A contract test that reads the generated JSON.
- Keeping the document accurate with examples, versioning and hidden internal routes.

Next: [v13: resilience with Polly](../v13-resilience/v13-resilience.md).

