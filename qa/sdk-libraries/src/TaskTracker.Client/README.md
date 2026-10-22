# TaskTracker.Client

A typed HTTP client for the TaskTracker API.

```csharp
var http = new HttpClient { BaseAddress = new Uri("https://localhost:5001/") };
var client = new TaskClient(http);

var created = await client.CreateTaskAsync("Write the chapter");
var tasks = await client.GetTasksAsync();
```

The client uses relative URLs, so `BaseAddress` must end with a slash. A non-success response throws `TaskClientException` with the status code and the response body.
