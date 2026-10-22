using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskApi.Tests;

[TestClass]
public sealed class GraphQlTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void StartServer()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void StopServer()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task MutationCreatesTaskAndQueryReadsIt()
    {
        var created = await PostAsync(
            "mutation { createTask(title: \"Write the chapter\") { id title } }");

        var task = created.GetProperty("createTask");
        var id = task.GetProperty("id").GetString();

        Assert.AreEqual("Write the chapter", task.GetProperty("title").GetString());

        var read = await PostAsync($"{{ task(id: \"{id}\") {{ id title }} }}");

        Assert.AreEqual(id, read.GetProperty("task").GetProperty("id").GetString());
        Assert.AreEqual("Write the chapter", read.GetProperty("task").GetProperty("title").GetString());
    }

    [TestMethod]
    public async Task MissingTaskReturnsNull()
    {
        var data = await PostAsync($"{{ task(id: \"{Guid.NewGuid()}\") {{ id }} }}");

        Assert.AreEqual(JsonValueKind.Null, data.GetProperty("task").ValueKind);
    }

    private async Task<JsonElement> PostAsync(string query)
    {
        var response = await _client.PostAsJsonAsync("/graphql", new { query });
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsFalse(
            payload.TryGetProperty("errors", out var errors),
            errors.ToString());

        return payload.GetProperty("data");
    }
}
