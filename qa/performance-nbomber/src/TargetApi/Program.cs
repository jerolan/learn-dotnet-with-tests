var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/ping", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program
{
}

