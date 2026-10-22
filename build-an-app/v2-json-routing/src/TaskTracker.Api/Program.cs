using System.Text.Json;
using System.Text.Json.Serialization;
using TaskTracker.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryTaskStore>();
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

app.MapPost("/tasks", (CreateTaskRequest request, InMemoryTaskStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new ErrorResponse("Title is required."));
    }

    var task = store.Add(request.Title.Trim());

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

