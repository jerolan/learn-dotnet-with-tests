using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskApi.Tests;

[TestClass]
public sealed class TaskHubTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [TestInitialize]
    public void StartServer()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [TestCleanup]
    public void StopServer()
    {
        _factory.Dispose();
    }

    [TestMethod]
    public async Task ConnectingClientReceivesItsConnectionId()
    {
        var connected = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = await ConnectAsync(
            hub => hub.On<string>("Connected", id => connected.TrySetResult(id)));

        var id = await connected.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(connection.ConnectionId, id);
    }

    [TestMethod]
    public async Task ConnectedClientReceivesCreatedTask()
    {
        var received = new TaskCompletionSource<JsonElement>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = await ConnectAsync(
            hub => hub.On<JsonElement>("TaskCreated", task => received.TrySetResult(task)));

        var task = new { id = Guid.NewGuid(), title = "Write the chapter" };
        var response = await _factory.CreateClient().PostAsJsonAsync("/tasks", task);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var payload = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(task.id, payload.GetProperty("id").GetGuid());
        Assert.AreEqual("Write the chapter", payload.GetProperty("title").GetString());
    }

    private async Task<HubConnection> ConnectAsync(Action<HubConnection> beforeStart)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "hubs/tasks"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        beforeStart(connection);
        await connection.StartAsync();

        return connection;
    }
}
