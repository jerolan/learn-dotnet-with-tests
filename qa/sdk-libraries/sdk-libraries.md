# SDK libraries

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/sdk-libraries)

Three applications call the same task API. Each one sets a base address, chooses JSON options, checks the status and reads the body in its own way. The differences are small until one of them sends `TaskId` where the API expects `taskId`, or forgets to dispose a response.

A typed client packaged as a NuGet library moves those decisions into one place. This appendix builds `TaskTracker.Client`, tests it without a network, and packs it into a `.nupkg`.

## How it works

The package wraps an `HttpClient` that the caller configures. The library keeps the API routes, JSON names and error conversion in one place; package metadata controls what consumers receive.

### The project

`src/TaskTracker.Client/TaskTracker.Client.csproj`:

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <PackageId>TaskTracker.Client</PackageId>
  <Version>1.0.0</Version>
  <Authors>Learn .NET with Tests</Authors>
  <Description>Typed HTTP client for the TaskTracker API.</Description>
  <PackageTags>tasks;http;client</PackageTags>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>

<ItemGroup>
  <None Include="README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

`GenerateDocumentationFile` turns the XML comments into a file that ships with the assembly, so IDE tooltips work for consumers. `PackageReadmeFile` puts the README on the package page. Neither changes the code; both change what a consumer receives.

### The client

`src/TaskTracker.Client/TaskClient.cs`:

```csharp
public sealed class TaskClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public TaskClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("tasks", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<TaskDto>>(
            JsonOptions,
            cancellationToken) ?? new List<TaskDto>();
    }

    public async Task<TaskDto> CreateTaskAsync(
        string title,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(
            "tasks",
            new { title },
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<TaskDto>(
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty body.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new TaskClientException(
            response.StatusCode,
            $"The API returned {(int)response.StatusCode}: {body}");
    }
}
```

The client takes an `HttpClient` instead of creating one. The caller owns its lifetime, its base address and its handler pipeline; the library owns the URLs, the JSON options and the error shape.

## How to test

The tests replace the network with a `StubHandler`, so they need no server or open port. Run `dotnet test SdkLibraries.sln` from this appendix folder.

## Write the test first

The current client is the starting point. The test comes first for the route change because the URL is part of the API contract.

The test replaces the network with a handler that records the request:

```csharp
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
```

`StubHandler` derives from `HttpMessageHandler` and returns a fixed response:

```csharp
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
```

No server, no port and no network. The test still checks the method, the URL and the deserialization.

## Try to run the test

Point the client at `task` instead of `tasks` and the test catches it:

```text
Assert.AreEqual failed. Expected:<https://api.test/tasks>. Actual:<https://api.test/task>.
```

The URL is part of the contract, so it belongs in an assertion. A test that only checks the returned object would pass against a stub that ignores the path.

## Write the minimal amount of code for the test to run and check the failing test output

The client can deserialize the response with this request line, but the URL is wrong:

```csharp
using var response = await _http.GetAsync("task", cancellationToken);
```

## Write enough code to make it pass

Use the plural route the API exposes:

```csharp
using var response = await _http.GetAsync("tasks", cancellationToken);
```

Run the solution again; the read, write and error tests pass.

## Refactor

### Pack it

```sh
dotnet pack src/TaskTracker.Client/TaskTracker.Client.csproj -c Release -o /tmp/sdk-pack
```

```text
Successfully created package '/tmp/sdk-pack/TaskTracker.Client.1.0.0.nupkg'.
```

The package is 9 KB and contains:

```text
lib/net6.0/TaskTracker.Client.dll
lib/net6.0/TaskTracker.Client.xml
README.md
TaskTracker.Client.nuspec
```

The `.nuspec` is generated from the project file:

```xml
<id>TaskTracker.Client</id>
<version>1.0.0</version>
<authors>Learn .NET with Tests</authors>
<readme>README.md</readme>
<description>Typed HTTP client for the TaskTracker API.</description>
```

### Versioning the public surface

The package id and version are part of the contract. Removing a method, changing a return type or renaming a record property is a breaking change; it needs a major version. Adding an optional parameter or a new method is a minor version.

The XML documentation file and the README ship inside the package, so a consumer sees the same description you wrote here. A package that only contains a DLL forces every consumer to read the source.

### Production concerns

- Use `IHttpClientFactory` or a typed client registration in the consuming app. Creating a new `HttpClient` per call exhausts sockets.
- Put authentication in a `DelegatingHandler`, not in every method. The token refresh belongs there.
- Add a timeout and a cancellation token to every call. The library should not decide that a request may hang forever.
- Generate Source Link and deterministic builds if the package is published; a consumer should be able to step into the source.
- Run `dotnet pack` in CI and keep the `.nupkg` as an artifact. The package is the deliverable, not a side effect of a local build.
- Add a compatibility check before a release, such as `Microsoft.DotNet.ApiCompat`, so a removed public member fails the build.

## Repeat for new requirements

The next requirements are creating a task and mapping an error response. The second test returns `201 Created` and asserts the POST:

```csharp
Assert.AreEqual(HttpMethod.Post, handler.Request!.Method);
StringAssert.Contains(handler.RequestBody, "Write the chapter");
Assert.AreEqual("application/json", handler.Request.Content!.Headers.ContentType!.MediaType);
```

The third returns `400 Bad Request` and expects the typed exception:

```csharp
var error = await Assert.ThrowsExceptionAsync<TaskClientException>(
    () => client.CreateTaskAsync(""));

Assert.AreEqual(HttpStatusCode.BadRequest, error.StatusCode);
StringAssert.Contains(error.Message, "title is required");
```

`TaskClientException` carries the status and the body. A caller can show the message or map the status without parsing a string.

## What is deferred

The package is not pushed to nuget.org or to a private feed, and it is not signed. A hands-on lab would add a `NUGET_API_KEY` secret, a `dotnet nuget push` step gated on a tag, and a smoke test that installs the package into a fresh console project.

## Wrapping up

What we have covered:

- A class library with package metadata, a README and XML documentation.
- A typed client that owns URLs, JSON options and error handling.
- Three tests against a stub handler: read, write and failure.
- The URL assertion that catches a wrong path.
- `dotnet pack`, the files inside the package, and the versioning rules for its public surface.
- Lifetime, auth handlers, Source Link and CI packaging as production concerns.

Next: end-to-end testing with Playwright.
