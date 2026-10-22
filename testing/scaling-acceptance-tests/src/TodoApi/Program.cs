var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<Todo>();

app.MapGet("/todos", () => todos);

app.MapPost("/todos", (Todo todo) =>
{
    if (string.IsNullOrWhiteSpace(todo.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }

    if (todos.Any(existing => existing.Id == todo.Id))
    {
        return Results.Conflict(new { error = "A todo with that id already exists." });
    }

    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.Run();

public sealed record Todo(int Id, string Title);

public partial class Program
{
}

