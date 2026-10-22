using TaskTracker.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryTaskStore>();

var app = builder.Build();

app.MapGet("/tasks", (InMemoryTaskStore store) => store.All());

app.MapGet("/tasks/{id:int}", (int id, InMemoryTaskStore store) =>
{
    var task = store.Find(id);

    return task is null ? Results.NotFound() : Results.Ok(task);
});

app.MapPost("/tasks", (CreateTaskRequest request, InMemoryTaskStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }

    var task = store.Add(request.Title.Trim());

    return Results.Created($"/tasks/{task.Id}", task);
});

app.Run();

public sealed record CreateTaskRequest(string Title);

public partial class Program
{
}

