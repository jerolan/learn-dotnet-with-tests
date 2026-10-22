using TaskApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddSingleton<TaskGrpcService>();

var app = builder.Build();

app.MapGrpcService<TaskGrpcService>();
app.MapGet("/", () => Results.Text("gRPC endpoint: use a gRPC client."));

app.Run();

public partial class Program
{
}
