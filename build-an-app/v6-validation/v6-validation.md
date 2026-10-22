# v6: Validation with FluentValidation

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v6-validation)

Version 5 validated the title with two guards inside the POST handler. Version 6 moves the rules into a validator and returns a standard validation problem with per-field errors.

The finished solution is in `build-an-app/v6-validation`.

## Write the test first

```csharp
[TestMethod]
public async Task BlankTitle_ReturnsAnErrorBody()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest(" "));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

    var errors = await ValidationErrorsAsync(response);
    Assert.IsTrue(errors.TryGetProperty("Title", out var titleErrors));
    Assert.AreEqual("Title is required.", titleErrors[0].GetString());
}
```

The helper reads the standard Problem Details body:

```csharp
private static async Task<JsonElement> ValidationErrorsAsync(HttpResponseMessage response)
{
    using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    return document.RootElement.GetProperty("errors").Clone();
}
```

The response body has an `errors` object keyed by field name. A client can highlight the title input without parsing a human message.

## Add the package and the validator

```sh
dotnet add src/TaskTracker.Api package FluentValidation --version 11.9.2
```

```csharp
public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator(IOptions<TaskOptions> options)
    {
        var maxTitleLength = options.Value.MaxTitleLength;

        RuleFor(request => request.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title is required.")
            .Must(title => title.Trim().Length <= maxTitleLength)
            .WithMessage($"Title must be at most {maxTitleLength} characters.");
    }
}
```

Register it:

```csharp
builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
```

## Use it in the handler

```csharp
app.MapPost("/tasks", async (CreateTaskRequest request, ITaskWriter writer, IValidator<CreateTaskRequest> validator) =>
{
    var validation = await validator.ValidateAsync(request);

    if (!validation.IsValid)
    {
        var errors = validation.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }

    var task = writer.Add(request.Title.Trim());

    return Results.Created($"/tasks/{task.Id}", task);
});
```

Green. The handler has one validation branch and one happy path; the rules live in the validator.

## How it works

`AbstractValidator<T>` is the base class. `RuleFor` selects a property, `Must` adds a predicate and `WithMessage` sets the text. `CascadeMode.Stop` stops the chain after the first failure, so the length rule does not run on a null or whitespace title.

`IValidator<T>` comes from DI. The validator receives `IOptions<TaskOptions>`, so the maximum length is the same configured value the rest of the app uses. When the test overrides the configuration, the validator sees the override.

`Results.ValidationProblem(errors)` writes a `HttpValidationProblemDetails` with status 400, a standard `title` and the `errors` map. The group-by keeps multiple messages for the same field together.

## Repeat for new requirements

### The configured limit is a field error

The v3 test for a long title now reads the field error instead of a flat message:

```csharp
var errors = await ValidationErrorsAsync(response);
Assert.IsTrue(errors.TryGetProperty("Title", out var titleErrors));
StringAssert.Contains(titleErrors[0].GetString(), "10");
```

It passes with `maxTitleLength: 10`. The test proves the validator reads the option, not a hard-coded number.

### The appsettings default

The default test uses 121 characters and looks for `120` in the field error:

```csharp
StringAssert.Contains(titleErrors[0].GetString(), "120");
```

Green. The validator and the appsettings file agree.

## Refactor

A few boundaries keep validation from spreading:

- Validate at the request boundary. The domain and the repository can assume a valid command; they do not repeat the title rules.
- Keep one validator per request type. If two endpoints accept the same shape with different rules, use separate request records or explicit rule sets.
- Use `MaximumLength` for simple length; the custom `Must` here exists because the rule counts the trimmed title, which matches the stored value.
- Return the standard Problem Details shape. Clients and API gateways understand `errors`; a custom shape would need documentation and client code.

FluentValidation can also validate options or domain objects, but the request DTO is the place where user input enters the system. That is the boundary this chapter protects.

## Wrapping up

What we have covered:

- A FluentValidation validator with `RuleFor`, `Must`, `WithMessage` and `CascadeMode.Stop`.
- A validator that reads the same `TaskOptions` as the app.
- `IValidator<T>` injected into a minimal API handler.
- Grouping failures by property into `Results.ValidationProblem`.
- A standard `errors` map that clients can consume.
- Moving validation out of the handler without losing the tested behavior.

Next: [v7: JWT authentication](../v7-auth/v7-auth.md).

