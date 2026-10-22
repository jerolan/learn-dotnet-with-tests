using System.Runtime.CompilerServices;

namespace TaskStream;

public sealed class TaskSource
{
    private readonly IReadOnlyList<TaskItem> _tasks;

    public TaskSource(IReadOnlyList<TaskItem> tasks)
    {
        _tasks = tasks;
    }

    public async IAsyncEnumerable<TaskItem> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var task in _tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return task;
        }
    }
}
