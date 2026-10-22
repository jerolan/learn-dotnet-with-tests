using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TaskTracker.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryTaskStore>();
builder.Services.Configure<TaskOptions>(builder.Configuration.GetSection(TaskOptions.SectionName));
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

app.MapGet("/tasks", (InMemoryTaskStore store) => store.All());

app.MapGet("/tasks/{id:int}", (int id, InMemoryTaskStore store) =>
{
    var task = store.Find(id);

    return task is null
        ? Results.NotFound(new ErrorResponse($"Task {id} was not found."))
        : Results.Ok(task);
});

app.MapPost("/tasks", (CreateTaskRequest request, InMemoryTaskStore store, IOptions<TaskOptions> options) =>
{
    var maxTitleLength = options.Value.MaxTitleLength;

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new ErrorResponse("Title is required."));
    }

    var title = request.Title.Trim();

    if (title.Length > maxTitleLength)
    {
        return Results.BadRequest(new ErrorResponse($"Title must be at most {maxTitleLength} characters."));
    }

    var task = store.Add(title);

    return Results.Created($"/tasks/{task.Id}", task);
});

app.MapMethods("/tasks/{id:int}/done", new[] { HttpMethods.Patch }, (int id, InMemoryTaskStore store) =>
{
    var task = store.MarkDone(id);

    return task is null
        ? Results.NotFound(new ErrorResponse($"Task {id} was not found."))
        : Results.NoContent();
});

app.Run();

public sealed record CreateTaskRequest(string Title);

public sealed record TaskItem(int Id, string Title, bool IsDone);

public sealed record ErrorResponse(string Error);

public partial class Program
{
}

