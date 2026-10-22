using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskTracker.Client.Tests;

[TestClass]
public sealed class TaskClientTests
{
    [TestMethod]
    public async Task GetTasksReadsTheListFromTheApi()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler(
            HttpStatusCode.OK,
            $"[{{\"id\":\"{id}\",\"title\":\"Write the chapter\",\"isDone\":false}}]");

        var client = CreateClient(handler);

        var tasks = await client.GetTasksAsync();

        Assert.AreEqual(1, tasks.Count);
        Assert.AreEqual(id, tasks[0].Id);
        Assert.AreEqual("Write the chapter", tasks[0].Title);
        Assert.AreEqual(HttpMethod.Get, handler.Request!.Method);
        Assert.AreEqual("https://api.test/tasks", handler.Request.RequestUri!.ToString());
    }

    [TestMethod]
    public async Task CreateTaskPostsTheTitleAndReadsTheCreatedTask()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler(
            HttpStatusCode.Created,
            $"{{\"id\":\"{id}\",\"title\":\"Write the chapter\",\"isDone\":false}}");

        var client = CreateClient(handler);

        var created = await client.CreateTaskAsync("Write the chapter");

        Assert.AreEqual(id, created.Id);
        Assert.AreEqual(HttpMethod.Post, handler.Request!.Method);
        StringAssert.Contains(handler.RequestBody, "Write the chapter");
        Assert.AreEqual("application/json", handler.Request.Content!.Headers.ContentType!.MediaType);
    }

    [TestMethod]
    public async Task NonSuccessResponseCarriesTheStatusAndBody()
    {
        var handler = new StubHandler(
            HttpStatusCode.BadRequest,
            "{\"error\":\"title is required\"}");

        var client = CreateClient(handler);

        var error = await Assert.ThrowsExceptionAsync<TaskClientException>(
            () => client.CreateTaskAsync(""));

        Assert.AreEqual(HttpStatusCode.BadRequest, error.StatusCode);
        StringAssert.Contains(error.Message, "title is required");
    }

    private static TaskClient CreateClient(HttpMessageHandler handler)
    {
        return new TaskClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.test/")
        });
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public HttpRequestMessage? Request { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;

            if (request.Content is not null)
            {
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }
    }
}
