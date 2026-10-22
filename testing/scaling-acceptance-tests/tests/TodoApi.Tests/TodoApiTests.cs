using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TodoApi.Tests;

[TestClass]
public sealed class TodoApiTests
{
    [TestMethod]
    public async Task RejectsATodoWithoutATitle()
    {
        using var api = new Api();

        var response = await api.Client.CreateAsync(new Todo(1, " "));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ListsTodosInCreationOrder()
    {
        using var api = new Api();

        await api.Client.CreateAsync(new Todo(1, "First"));
        await api.Client.CreateAsync(new Todo(2, "Second"));

        var todos = await api.Client.ListAsync();

        Assert.AreEqual(2, todos.Count);
        Assert.AreEqual("First", todos[0].Title);
        Assert.AreEqual("Second", todos[1].Title);
    }

    [TestMethod]
    public async Task RejectsADuplicateId()
    {
        using var api = new Api();

        await api.Client.CreateAsync(new Todo(1, "First"));
        var response = await api.Client.CreateAsync(new Todo(1, "Duplicate"));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    private sealed class Api : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory = new();

        public Api()
        {
            Client = new TodoClient(_factory.CreateClient());
        }

        public TodoClient Client { get; }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }

    private sealed class TodoClient
    {
        private readonly HttpClient _client;

        public TodoClient(HttpClient client)
        {
            _client = client;
        }

        public Task<HttpResponseMessage> CreateAsync(Todo todo)
        {
            return _client.PostAsJsonAsync("/todos", todo);
        }

        public async Task<List<Todo>> ListAsync()
        {
            var todos = await _client.GetFromJsonAsync<List<Todo>>("/todos");
            return todos!;
        }
    }
}

