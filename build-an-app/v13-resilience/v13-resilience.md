# v13: Resilience with Polly

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v13-resilience)

An outbound dependency will fail sometimes. A retry can turn a transient 503 into a success; it can also multiply the damage when the failure is permanent. Version 13 wraps the notifier in a Polly retry policy and tests the budget with a handler that fails on purpose.

The finished solution is in `build-an-app/v13-resilience`.

## How it works

The endpoint calls an `INotifier` adapter that sends an HTTP request. A decorator runs that call through a Polly policy, which decides which exceptions to retry and how many times. Keeping the policy outside `HttpNotifier` separates the HTTP request from the resilience choice.

## How to test

Drive the notification endpoint through HTTP and replace the outbound transport with a fake `HttpMessageHandler`. The handler returns a controlled sequence of `503` and `200` responses and counts attempts, so the test can assert both the endpoint response and the retry budget without a real service.

## Write the test first

```csharp
[TestMethod]
public async Task Notifications_RetryTransientFailures()
{
    using var api = new Api(notifierFailures: 2);

    var response = await api.Client.PostAsJsonAsync(
        "/notifications",
        new NotificationRequest("hello"));

    Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    Assert.AreEqual(3, api.NotifierCalls);
}
```

The test host uses a fake HTTP handler that returns `503` for the first two calls and `200` afterwards. It counts every attempt, including the first.

## Try to run the test

The test does not compile yet: `NotificationRequest` is missing, and the test `Api` helper does not accept `notifierFailures` or expose `NotifierCalls`.

## Write the minimal amount of code for the test to run and check the failing test output

Add the request type and a fake handler to the test fixture. `Api(notifierFailures: 2)` configures this handler to fail twice and exposes its call count:

```csharp
public sealed record NotificationRequest(string Message);
```

```csharp
private sealed class FlakyHandler : HttpMessageHandler
{
    private readonly int _failuresBeforeSuccess;

    public FlakyHandler(int failuresBeforeSuccess)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Calls++;
        var status = Calls <= _failuresBeforeSuccess
            ? HttpStatusCode.ServiceUnavailable
            : HttpStatusCode.OK;

        return Task.FromResult(new HttpResponseMessage(status));
    }
}
```

The test now runs, but `/notifications` returns 404 because the application has no notification endpoint yet.

## Write enough code to make it pass

Install Polly and add an HTTP adapter for the outbound request:

```sh
dotnet add src/TaskTracker.Api package Polly --version 7.2.4
```

```csharp
public interface INotifier
{
    Task SendAsync(string message);
}

public sealed class HttpNotifier : INotifier
{
    private readonly HttpClient _client;

    public HttpNotifier(HttpClient client)
    {
        _client = client;
    }

    public async Task SendAsync(string message)
    {
        using var response = await _client.PostAsJsonAsync("/notify", new { message });
        response.EnsureSuccessStatusCode();
    }
}
```

`EnsureSuccessStatusCode` throws `HttpRequestException` for a 5xx. Wrap the adapter in a retry decorator:

```csharp
public sealed class ResilientNotifier : INotifier
{
    private readonly INotifier _inner;
    private readonly IAsyncPolicy _policy;

    public ResilientNotifier(INotifier inner, IAsyncPolicy policy)
    {
        _inner = inner;
        _policy = policy;
    }

    public Task SendAsync(string message)
    {
        return _policy.ExecuteAsync(() => _inner.SendAsync(message));
    }
}
```

Register the typed client and the decorator, then map the endpoint:

```csharp
builder.Services.AddHttpClient<HttpNotifier>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Notifier:BaseUrl"] ?? "http://notifier"));

builder.Services.AddScoped<INotifier>(services =>
    new ResilientNotifier(
        services.GetRequiredService<HttpNotifier>(),
        Policy.Handle<HttpRequestException>().RetryAsync(3)));
```

```csharp
app.MapPost("/notifications", async (NotificationRequest request, INotifier notifier) =>
{
    await notifier.SendAsync(request.Message);

    return Results.Accepted();
}).RequireAuthorization();
```

The test host registers the same decorator with an `HttpClient` backed by `FlakyHandler` and starts with the same three-retry limit, so no live notifier is needed. The test passes: the first two attempts fail with `503`, and the third succeeds.

## Refactor

A fixed `RetryAsync` count is a teaching policy. Production needs more thought:

- Use exponential backoff with jitter so a failing dependency is not hammered by every client at once.
- Add a timeout per attempt. A retry around a call that hangs forever is not resilience.
- Add a circuit breaker after repeated failures so the caller fails fast while the dependency recovers.
- Retry only transient failures: timeouts, 5xx responses and connection errors. A 400 or a validation error will not improve on the second attempt.
- Log the attempt number and the correlation id. A retry that nobody can see is hard to diagnose.
- Use the typed `HttpClient` from the factory; do not create a new `HttpClient` per call.

A retry is only safe when the operation is idempotent. Sending a notification twice may annoy a user; charging a card twice is unacceptable. For non-idempotent calls, use an idempotency key and a server-side deduplication contract.

Polly 8 replaces the v7 policy API with resilience strategies composed in a `ResiliencePipeline`. The v7 API remains available through the `Polly` package ([Polly migration guide](https://github.com/App-vNext/Polly/blob/main/docs/migration-v8.md)).

## Repeat for new requirements

### Give up after the budget

```csharp
[TestMethod]
public async Task Notifications_GiveUpAfterTheRetryBudget()
{
    using var api = new Api(notifierFailures: 10);

    var response = await api.Client.PostAsJsonAsync(
        "/notifications",
        new NotificationRequest("hello"));

    Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    Assert.AreEqual(3, api.NotifierCalls);
}
```

With the current three-retry budget, the endpoint returns 500, but the handler records four calls and fails the count assertion. Reduce the retry count to two in both the production and test host registrations:

```csharp
Policy.Handle<HttpRequestException>().RetryAsync(2)
```

The test passes with one initial attempt plus two retries. Ten configured failures are more than the policy allows, so the third failure escapes and the endpoint returns 500.

### No retry on success

```csharp
[TestMethod]
public async Task Notifications_DoNotRetrySuccess()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync(
        "/notifications",
        new NotificationRequest("hello"));

    Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    Assert.AreEqual(1, api.NotifierCalls);
}
```

This test also passes without a code change: the policy returns a successful call immediately.

## Wrapping up

What we have covered:

- A typed `HttpClient` notifier behind an interface.
- A Polly retry decorator with a bounded budget.
- A fake handler that controls failures and counts attempts.
- Tests for recovery, exhaustion and the success path.
- Idempotency, backoff, timeout, circuit breaker and observability as the next steps.

Next: [v14: CQRS with MediatR](../v14-cqrs/v14-cqrs.md).
