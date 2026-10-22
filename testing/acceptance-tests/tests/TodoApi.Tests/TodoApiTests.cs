using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TodoApi.Tests;

[TestClass]
public sealed class TodoApiTests
{
    [TestMethod]
    public async Task CanCreateAndListTodos()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/todos", new Todo(1, "Write tests"));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");

        Assert.IsNotNull(todos);
        Assert.AreEqual(1, todos.Count);
        Assert.AreEqual("Write tests", todos[0].Title);
    }

    [TestMethod]
    public async Task EmptyList_ReturnsNoTodos()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");

        Assert.IsNotNull(todos);
        Assert.AreEqual(0, todos.Count);
    }
}

