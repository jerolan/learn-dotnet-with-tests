using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskTracker.Api.Tests;

[TestClass]
public sealed class TaskApiTests
{
    [TestMethod]
    public async Task CanCreateAndGetATask()
    {
        using var api = new Api();

        var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TaskItem>();
        Assert.IsNotNull(created);
        Assert.AreEqual(1, created.Id);
        Assert.AreEqual("Write tests", created.Title);
        Assert.IsFalse(created.IsDone);

        var fetched = await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{created.Id}");

        Assert.IsNotNull(fetched);
        Assert.AreEqual(created, fetched);
    }

    [TestMethod]
    public async Task List_ReturnsCreatedTasks()
    {
        using var api = new Api();

        await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("First"));
        await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Second"));

        var tasks = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

        Assert.IsNotNull(tasks);
        Assert.AreEqual(2, tasks.Count);
        Assert.AreEqual("First", tasks[0].Title);
        Assert.AreEqual("Second", tasks[1].Title);
    }

    [TestMethod]
    public async Task UnknownTask_ReturnsNotFound()
    {
        using var api = new Api();

        var response = await api.Client.GetAsync("/tasks/99");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task BlankTitle_ReturnsBadRequest()
    {
        using var api = new Api();

        var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest(" "));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class Api : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory = new();

        public Api()
        {
            Client = _factory.CreateClient();
        }

        public HttpClient Client { get; }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }
}

