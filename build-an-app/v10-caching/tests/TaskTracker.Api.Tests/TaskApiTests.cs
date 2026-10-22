using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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

    [TestMethod]
    public async Task TasksWithoutToken_ReturnUnauthorized()
    {
        using var api = new Api(authenticate: false);

        var response = await api.Client.GetAsync("/tasks");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task TokenEndpoint_WithInvalidCredentials_ReturnsUnauthorized()
    {
        using var api = new Api(authenticate: false);

        var response = await api.Client.PostAsJsonAsync(
            "/auth/token",
            new LoginRequest("admin", "wrong"));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task TokenEndpoint_ReturnsATokenThatAuthorizesTheApi()
    {
        using var api = new Api(authenticate: false);

        var token = await api.GetTokenAsync();
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));

        api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await api.Client.GetAsync("/tasks");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Diagnostics_UsesAScopedRequestIdAndASingletonAppId()
    {
        using var api = new Api();

        var first = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");
        var second = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreEqual(first.AppId, second.AppId);
        Assert.AreNotEqual(first.RequestId, second.RequestId);
    }

    [TestMethod]
    public async Task ReadCounter_IsIncrementedByTheDecorator()
    {
        using var api = new Api();

        var before = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");
        await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");
        var after = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

        Assert.IsNotNull(before);
        Assert.IsNotNull(after);
        Assert.AreEqual(before.ReadCount + 1, after.ReadCount);
    }

    [TestMethod]
    public async Task CreateTask_LogsTaskIdAndTitle()
    {
        using var api = new Api();

        var response = await api.Client.PostAsJsonAsync(
            "/tasks",
            new CreateTaskRequest("Write tests"));

        var task = await response.Content.ReadFromJsonAsync<TaskItem>();
        var record = api.Logs.Single(entry => entry.Message.Contains("created"));

        Assert.AreEqual(task!.Id, record.Fields["TaskId"]);
        Assert.AreEqual("Write tests", record.Fields["Title"]);
    }

    [TestMethod]
    public async Task CorrelationId_FlowsIntoTheResponseAndTheScope()
    {
        using var api = new Api();
        var request = new HttpRequestMessage(HttpMethod.Get, "/tasks");
        request.Headers.Add("X-Correlation-Id", "test-123");

        var response = await api.Client.SendAsync(request);

        Assert.AreEqual("test-123", response.Headers.GetValues("X-Correlation-Id").Single());

        var record = api.Logs.Last(entry => entry.Message.Contains("Handled"));
        Assert.AreEqual("test-123", record.Fields["CorrelationId"]);
    }

    [TestMethod]
    public async Task RepeatedListReads_HitTheCache()
    {
        using var api = new Api();
        await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));

        var before = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

        await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");
        await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

        var after = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

        Assert.AreEqual(before!.ReadCount + 1, after!.ReadCount);
    }

    [TestMethod]
    public async Task Write_InvalidatesTheListCache()
    {
        using var api = new Api();
        await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("First"));

        var first = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");
        await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Second"));
        var second = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

        Assert.AreEqual(1, first!.Count);
        Assert.AreEqual(2, second!.Count);
    }

    [TestMethod]
    public async Task MarkDone_InvalidatesBothCachedEntries()
    {
        using var api = new Api();
        var created = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
        var task = await created.Content.ReadFromJsonAsync<TaskItem>();

        await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{task!.Id}");
        await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

        await api.Client.PatchAsync($"/tasks/{task.Id}/done", null);

        var fetched = await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{task.Id}");
        var list = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

        Assert.IsTrue(fetched!.IsDone);
        Assert.IsTrue(list!.Single(item => item.Id == task.Id).IsDone);
    }

    private sealed class Api : IDisposable
    {
        private readonly SqliteConnection? _connection;
        private readonly WebApplicationFactory<Program> _factory;
        private readonly RecordingLoggerProvider _logs = new();

        public Api(bool authenticate = true, string? databasePath = null, int? maxTitleLength = null)
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

                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(_logs);
                    logging.SetMinimumLevel(LogLevel.Information);
                });

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

            if (authenticate)
            {
                var token = GetTokenAsync().GetAwaiter().GetResult();
                Client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public HttpClient Client { get; }

        public IReadOnlyCollection<LogRecord> Logs => _logs.Records.ToArray();

        public async Task<string> GetTokenAsync(string username = "admin", string password = "password")
        {
            var response = await Client.PostAsJsonAsync(
                "/auth/token",
                new LoginRequest(username, password));

            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>();

            return token!.AccessToken;
        }

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

    private sealed record LogRecord(
        string Category,
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object?> Fields);

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<LogRecord> Records { get; } = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new RecordingLogger(categoryName, Records);
        }

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentQueue<LogRecord> _records;
        private readonly AsyncLocal<Dictionary<string, object?>?> _scopes = new();

        public RecordingLogger(string category, ConcurrentQueue<LogRecord> records)
        {
            _category = category;
            _records = records;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            var previous = _scopes.Value;
            var current = previous is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(previous);

            if (state is IEnumerable<KeyValuePair<string, object?>> fields)
            {
                foreach (var field in fields)
                {
                    current[field.Key] = field.Value;
                }
            }

            _scopes.Value = current;

            return new Scope(() => _scopes.Value = previous);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var fields = new Dictionary<string, object?>();

            if (_scopes.Value is not null)
            {
                foreach (var field in _scopes.Value)
                {
                    fields[field.Key] = field.Value;
                }
            }

            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                foreach (var field in values)
                {
                    fields[field.Key] = field.Value;
                }
            }

            _records.Enqueue(new LogRecord(
                _category,
                logLevel,
                formatter(state, exception),
                fields));
        }

        private sealed class Scope : IDisposable
        {
            private readonly Action _onDispose;

            public Scope(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose();
            }
        }
    }
}

