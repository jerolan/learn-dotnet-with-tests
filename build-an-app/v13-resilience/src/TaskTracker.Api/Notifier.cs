using System.Net.Http.Json;
using Polly;

namespace TaskTracker.Api;

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

