using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskTracker.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskTracker")));
builder.Services.AddScoped<ITaskReader, DapperTaskReader>();
builder.Services.AddScoped<ITaskWriter, EfTaskWriter>();
builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
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

