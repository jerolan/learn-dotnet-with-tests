using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        var errors = await ValidationErrorsAsync(response);
        Assert.IsTrue(errors.TryGetProperty("Title", out var titleErrors));
        Assert.AreEqual("Title is required.", titleErrors[0].GetString());
    }

    [TestMethod]
    public async Task TitleLongerThanTheConfiguredLimit_ReturnsBadRequest()
    {
        using var api = new Api(maxTitleLength: 10);

        var response = await api.Client.PostAsJsonAsync(
            "/tasks",
            new CreateTaskRequest(new string('a', 11)));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var errors = await ValidationErrorsAsync(response);
        Assert.IsTrue(errors.TryGetProperty("Title", out var titleErrors));
        StringAssert.Contains(titleErrors[0].GetString(), "10");
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

        var errors = await ValidationErrorsAsync(response);
        Assert.IsTrue(errors.TryGetProperty("Title", out var titleErrors));
        StringAssert.Contains(titleErrors[0].GetString(), "120");
    }

    private static async Task<JsonElement> ValidationErrorsAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("errors").Clone();
    }

    [TestMethod]
    public async Task TasksSurviveAnAppRestart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tasktracker-{Guid.NewGuid():N}.db");

        try
        {
            int id;

            using (var first = new Api(databasePath: path))
            {
                var response = await first.Client.PostAsJsonAsync(
                    "/tasks",
                    new CreateTaskRequest("Persist me"));

                var task = await response.Content.ReadFromJsonAsync<TaskItem>();
                id = task!.Id;
            }

            using var second = new Api(databasePath: path);

            var fetched = await second.Client.GetFromJsonAsync<TaskItem>($"/tasks/{id}");

            Assert.IsNotNull(fetched);
            Assert.AreEqual("Persist me", fetched.Title);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    public async Task Get_SeesRowsWrittenOutsideEf()
    {
        using var api = new Api();

        await api.ExecuteAsync("INSERT INTO Tasks (Title, IsDone) VALUES ('Direct', 0);");

        var tasks = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

        Assert.IsNotNull(tasks);
        Assert.AreEqual(1, tasks.Count);
        Assert.AreEqual("Direct", tasks[0].Title);
    }

    private sealed class Api : IDisposable
    {
        private readonly SqliteConnection? _connection;
        private readonly WebApplicationFactory<Program> _factory;

        public Api(string? databasePath = null, int? maxTitleLength = null)
        {
            if (databasePath is null)
            {
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();
            }

            var factory = new WebApplicationFactory<Program>();

            factory = factory.WithWebHostBuilder(builder =>
            {
                if (maxTitleLength.HasValue)
                {
                    builder.ConfigureAppConfiguration((_, config) =>
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Tasks:MaxTitleLength"] = maxTitleLength.Value.ToString()
                        }));
                }

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<TaskDbContext>>();
                    services.AddDbContext<TaskDbContext>(options =>
                    {
                        if (_connection is not null)
                        {
                            options.UseSqlite(_connection);
                        }
                        else
                        {
                            options.UseSqlite($"Data Source={databasePath}");
                        }
                    });
                });
            });

            _factory = factory;
            Client = _factory.CreateClient();
        }

        public HttpClient Client { get; }

        public async Task ExecuteAsync(string sql)
        {
            if (_connection is null)
            {
                throw new InvalidOperationException("Direct SQL needs the in-memory test database.");
            }

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            await command.ExecuteNonQueryAsync();
        }

        public void Dispose()
        {
            _factory.Dispose();
            _connection?.Dispose();
        }
    }
}

