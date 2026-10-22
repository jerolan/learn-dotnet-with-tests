using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskApi.Grpc;

namespace TaskApi.Tests;

[TestClass]
public sealed class TaskGrpcTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private GrpcChannel _channel = null!;
    private TaskService.TaskServiceClient _client = null!;

    [TestInitialize]
    public void StartServer()
    {
        _factory = new WebApplicationFactory<Program>();
        _channel = GrpcChannel.ForAddress(_factory.Server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = _factory.Server.CreateHandler()
        });
        _client = new TaskService.TaskServiceClient(_channel);
    }

    [TestCleanup]
    public void StopServer()
    {
        _channel.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task CreatedTaskCanBeReadBack()
    {
        var created = await _client.CreateAsync(new CreateTaskRequest { Title = "Write the chapter" });

        var fetched = await _client.GetAsync(new GetTaskRequest { Id = created.Id });

        Assert.AreEqual(created.Id, fetched.Id);
        Assert.AreEqual("Write the chapter", fetched.Title);
    }

    [TestMethod]
    public async Task MissingTaskReturnsNotFound()
    {
        var error = await Assert.ThrowsExceptionAsync<RpcException>(async () =>
            await _client.GetAsync(new GetTaskRequest { Id = Guid.NewGuid().ToString() }));

        Assert.AreEqual(StatusCode.NotFound, error.StatusCode);
    }
}
