using System.Net.Http.Json;
using System.Text.Json;

namespace TaskTracker.Client;

/// <summary>A typed client for the TaskTracker task endpoints.</summary>
public sealed class TaskClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    /// <summary>Creates a client over an existing <see cref="HttpClient"/>.</summary>
    /// <param name="http">The client whose <see cref="HttpClient.BaseAddress"/> points at the API.</param>
    public TaskClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Reads every task.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The tasks returned by the API.</returns>
    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("tasks", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<TaskDto>>(
            JsonOptions,
            cancellationToken) ?? new List<TaskDto>();
    }

    /// <summary>Creates a task.</summary>
    /// <param name="title">The task title.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The created task.</returns>
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
