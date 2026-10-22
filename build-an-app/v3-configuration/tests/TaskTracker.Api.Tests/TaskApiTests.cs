using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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

    [TestMethod]
    public async Task Response_UsesCamelCaseJson()
    {
        using var api = new Api();

        var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
        var json = await response.Content.ReadAsStringAsync();

        StringAssert.Contains(json, "\"isDone\"");
        StringAssert.Contains(json, "\"title\"");
    }

    [TestMethod]
    public async Task MarkDone_ChangesTheTask()
    {
        using var api = new Api();
        var created = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
        var task = await created.Content.ReadFromJsonAsync<TaskItem>();

        var response = await api.Client.PatchAsync($"/tasks/{task!.Id}/done", null);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{task.Id}");
        Assert.IsNotNull(fetched);
        Assert.IsTrue(fetched.IsDone);
    }

    [TestMethod]
    public async Task MarkUnknownDone_ReturnsNotFoundWithAnError()
    {
        using var api = new Api();

        var response = await api.Client.PatchAsync("/tasks/99/done", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.IsNotNull(error);
        StringAssert.Contains(error.Error, "99");
    }

    [TestMethod]
    public async Task UnknownTask_ReturnsAnErrorBody()
    {
        using var api = new Api();

        var response = await api.Client.GetAsync("/tasks/99");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.IsNotNull(error);
        Assert.AreEqual("Task 99 was not found.", error.Error);
    }

    [TestMethod]
    public async Task BlankTitle_ReturnsAnErrorBody()
    {
        using var api = new Api();

        var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest(" "));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.IsNotNull(error);
        Assert.AreEqual("Title is required.", error.Error);
    }

    [TestMethod]
    public async Task TitleLongerThanTheConfiguredLimit_ReturnsBadRequest()
    {
        using var api = new Api(maxTitleLength: 10);

        var response = await api.Client.PostAsJsonAsync(
            "/tasks",
            new CreateTaskRequest(new string('a', 11)));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.IsNotNull(error);
        StringAssert.Contains(error.Error, "10");
    }

    [TestMethod]
    public async Task TitleAtTheConfiguredLimit_Succeeds()
    {
        using var api = new Api(maxTitleLength: 10);

        var response = await api.Client.PostAsJsonAsync(
            "/tasks",
            new CreateTaskRequest(new string('a', 10)));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    }

    [TestMethod]
    public async Task DefaultLimit_ComesFromAppSettings()
    {
        using var api = new Api();

        var response = await api.Client.PostAsJsonAsync(
            "/tasks",
            new CreateTaskRequest(new string('a', 121)));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.IsNotNull(error);
        StringAssert.Contains(error.Error, "120");
    }

    private sealed class Api : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;

        public Api(int? maxTitleLength = null)
        {
            var factory = new WebApplicationFactory<Program>();

            if (maxTitleLength.HasValue)
            {
                factory = factory.WithWebHostBuilder(builder =>
                    builder.ConfigureAppConfiguration((_, config) =>
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Tasks:MaxTitleLength"] = maxTitleLength.Value.ToString()
                        })));
            }

            _factory = factory;
            Client = _factory.CreateClient();
        }

        public HttpClient Client { get; }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }
}

