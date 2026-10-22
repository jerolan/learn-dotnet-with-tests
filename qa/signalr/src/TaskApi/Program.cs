using Microsoft.AspNetCore.SignalR;
using TaskApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/ping", () => Results.Ok(new { status = "ok" }));
app.MapHub<TaskHub>("/hubs/tasks");

app.MapPost("/tasks", async (TaskItem task, IHubContext<TaskHub> hub) =>
{
    await hub.Clients.All.SendAsync("TaskCreated", task);
    return Results.Created($"/tasks/{task.Id}", task);
});

app.Run();

public partial class Program
{
}
