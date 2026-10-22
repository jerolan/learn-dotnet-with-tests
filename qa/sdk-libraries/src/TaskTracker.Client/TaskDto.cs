namespace TaskTracker.Client;

/// <summary>A task returned by the TaskTracker API.</summary>
/// <param name="Id">The task identifier.</param>
/// <param name="Title">The task title.</param>
/// <param name="IsDone">Whether the task is complete.</param>
public sealed record TaskDto(Guid Id, string Title, bool IsDone);
