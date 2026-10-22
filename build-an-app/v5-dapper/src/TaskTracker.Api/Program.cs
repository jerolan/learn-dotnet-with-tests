using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskTracker.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskTracker")));
builder.Services.AddScoped<ITaskReader, DapperTaskReader>();
builder.Services.AddScoped<ITaskWriter, EfTaskWriter>();
builder.Services.Configure<TaskOptions>(builder.Configuration.GetSection(TaskOptions.SectionName));
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    db.Database.Migrate();
}

app.MapGet("/tasks", (ITaskReader reader) => reader.All());

app.MapGet("/tasks/{id:int}", (int id, ITaskReader reader) =>
{
    var task = reader.Find(id);

    return task is null
        ? Results.NotFound(new ErrorResponse($"Task {id} was not found."))
        : Results.Ok(task);
});

app.MapPost("/tasks", (CreateTaskRequest request, ITaskWriter writer, IOptions<TaskOptions> options) =>
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

    var task = writer.Add(title);

    return Results.Created($"/tasks/{task.Id}", task);
});

app.MapMethods("/tasks/{id:int}/done", new[] { HttpMethods.Patch }, (int id, ITaskWriter writer) =>
{
    var task = writer.MarkDone(id);

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

