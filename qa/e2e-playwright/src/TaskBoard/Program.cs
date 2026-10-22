using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var tasks = new ConcurrentDictionary<Guid, string>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/tasks", () =>
    tasks
        .Select(entry => new { id = entry.Key, title = entry.Value })
        .OrderBy(task => task.title));

app.MapPost("/api/tasks", (CreateTask request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }

    var id = Guid.NewGuid();
    var title = request.Title.Trim();
    tasks[id] = title;

    return Results.Created($"/api/tasks/{id}", new { id, title });
});

app.Run();

public sealed record CreateTask(string Title);

public partial class Program
{
}
