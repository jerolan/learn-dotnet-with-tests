# v13: Resilience with Polly

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v13-resilience)

An outbound dependency will fail sometimes. A retry can turn a transient 503 into a success; it can also multiply the damage when the failure is permanent. Version 13 wraps the notifier in a Polly retry policy and tests the budget with a handler that fails on purpose.

The finished solution is in `build-an-app/v13-resilience`.

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

The test host replaces the notifier with one that uses a fake handler. The handler returns `503` for the first two calls and `200` afterwards; the assertion expects the endpoint to succeed after three calls.

## Add the notifier

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

`EnsureSuccessStatusCode` throws `HttpRequestException` for a 5xx, which is the signal the policy handles.

## Wrap it in a policy

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

Register the typed client and the decorator:

```csharp
builder.Services.AddHttpClient<HttpNotifier>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Notifier:BaseUrl"] ?? "http://notifier"));

builder.Services.AddScoped<INotifier>(services =>
    new ResilientNotifier(
        services.GetRequiredService<HttpNotifier>(),
        Policy.Handle<HttpRequestException>().RetryAsync(2)));
```

Add the endpoint:

```csharp
app.MapPost("/notifications", async (NotificationRequest request, INotifier notifier) =>
{
    await notifier.SendAsync(request.Message);

    return Results.Accepted();
}).RequireAuthorization();
```

Green. The policy retries twice, so the third call is the one that succeeds.

## How it works

`Policy.Handle<HttpRequestException>()` says which failures are retryable. `RetryAsync(2)` runs the action again up to two times after the first failure. The handler counts every attempt, including the first.

The policy lives in the decorator, not in `HttpNotifier`. The HTTP client stays a plain adapter; the resilience decision is a separate concern and can change without touching the request code.

A retry is only safe when the operation is idempotent. Sending a notification twice may annoy a user; charging a card twice is unacceptable. For non-idempotent calls, use an idempotency key and a server-side deduplication contract before adding retries.

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

Green. The third failure escapes the policy and the endpoint returns 500. The test pins the attempt count: one initial call plus two retries, no more.

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

Green. The policy only observes failures; a successful call is returned immediately.

## Refactor

The hard-coded `RetryAsync(2)` is a teaching policy. Production needs more thought:

- Use exponential backoff with jitter so a failing dependency is not hammered by every client at once.
- Add a timeout per attempt. A retry around a call that hangs forever is not resilience.
- Add a circuit breaker after repeated failures so the caller fails fast while the dependency recovers.
- Retry only transient failures: timeouts, 5xx responses and connection errors. A 400 or a validation error will not improve on the second attempt.
- Log the attempt number and the correlation id. A retry that nobody can see is hard to diagnose.
- Use the typed `HttpClient` from the factory; do not create a new `HttpClient` per call.

Polly 8 exposes the same ideas through `ResiliencePipeline`. The policy shape is the same: handle selected failures, retry a bounded number of times, add a timeout and a breaker, and keep the policy out of the adapter.

## Wrapping up

What we have covered:

- A typed `HttpClient` notifier behind an interface.
- A Polly retry decorator with a bounded budget.
- A fake handler that controls failures and counts attempts.
- Tests for recovery, exhaustion and the success path.
- Idempotency, backoff, timeout, circuit breaker and observability as the next steps.

Next: v14: CQRS with MediatR.

