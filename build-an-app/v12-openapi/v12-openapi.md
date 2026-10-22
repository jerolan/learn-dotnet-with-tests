# v12: OpenAPI

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v12-openapi)

The API is now large enough that a caller needs a contract: which paths exist, which bodies they take, which responses they return and how to authenticate. Version 12 generates an OpenAPI document from the endpoints and tests that the document matches the surface.

The finished solution is in `build-an-app/v12-openapi`.

## How it works

ASP.NET Core's API explorer describes the minimal API routes. Swashbuckle reads that metadata and generates an OpenAPI document with paths and schemas. `UseSwagger` serves the JSON; `UseSwaggerUI` serves an interactive page for browsing it.

Authentication still comes from the app's middleware. The OpenAPI security scheme documents how a client sends a token; it does not protect a route.

## How to test

Request `/swagger/v1/swagger.json` over HTTP and parse the JSON. Check stable parts of the contract: route names, schema names and, in a follow-up test, the Bearer security scheme. This verifies the document consumers receive without testing Swashbuckle's formatting or relying on the UI.

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
}
```

## Try to run the test

Against v11, `GET /swagger/v1/swagger.json` returns 404 because the app does not expose an OpenAPI document. `GetStringAsync` fails on that non-success response.

## Write the minimal amount of code for the test to run and check the failing test output

Install Swashbuckle and register the API explorer and a named document:

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
});
```

The test still receives 404: the document is configured, but the middleware has not been added to the request pipeline.

## Write enough code to make it pass

Expose the document and UI:

```csharp
app.UseSwagger();
app.UseSwaggerUI();
```

The API surface test now passes: the JSON contains the expected paths and schemas.

## Refactor

A generated document is useful only while it stays accurate:

- Add XML documentation and examples when a field needs a human explanation. Enable `GenerateDocumentationFile` and include the file in `AddSwaggerGen`.
- Version the document when the API changes in a breaking way. `SwaggerDoc("v2", ...)` and a versioned route plan are cheaper than surprising clients.
- Hide internal endpoints. A diagnostics route with process ids is useful in development and should not be in a public document; use endpoint metadata or a separate document.
- Test the document in CI, as this chapter does. The UI is a convenience; the JSON is the contract.
- ASP.NET Core provides built-in OpenAPI document generation starting in .NET 9, while interactive UI is not included by default. This book uses Swashbuckle for its .NET 6 solution ([Microsoft documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)).

## Repeat for new requirements

### Describe Bearer authentication

Write a test for the authentication instructions in the document:

```csharp
[TestMethod]
public async Task SwaggerDocument_DescribesBearerAuthentication()
{
    using var api = new Api();

    var json = await api.Client.GetStringAsync("/swagger/v1/swagger.json");

    using var document = JsonDocument.Parse(json);
    var security = document.RootElement
        .GetProperty("components")
        .GetProperty("securitySchemes")
        .GetProperty("Bearer");

    Assert.AreEqual("http", security.GetProperty("type").GetString());
    Assert.AreEqual("bearer", security.GetProperty("scheme").GetString());
}
```

It fails because the generated document has no Bearer security scheme. Add the definition and requirement inside the existing `AddSwaggerGen` options block:

```csharp
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
```

The test passes. A client calls `/auth/token` without a token, then sends `Authorization: Bearer ...` to the task routes. The scheme documents that flow; the authentication middleware continues to enforce it.

## Wrapping up

What we have covered:

- `AddEndpointsApiExplorer` and `AddSwaggerGen` for minimal APIs.
- A documented API with paths, schemas and a Bearer security scheme.
- `UseSwagger` and the Swagger UI.
- A contract test that reads the generated JSON.
- Keeping the document accurate with examples, versioning and hidden internal routes.

Next: [v13: resilience with Polly](../v13-resilience/v13-resilience.md).
