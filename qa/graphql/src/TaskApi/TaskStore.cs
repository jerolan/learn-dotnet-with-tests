using System.Collections.Concurrent;

namespace TaskApi;

public sealed class TaskStore
{
    private readonly ConcurrentDictionary<Guid, TaskItem> _tasks = new();

    public TaskItem Add(string title)
    {
        var task = new TaskItem(Guid.NewGuid(), title);
        _tasks[task.Id] = task;
        return task;
    }

    public TaskItem? Find(Guid id)
    {
        return _tasks.TryGetValue(id, out var task) ? task : null;
    }

    public IReadOnlyList<TaskItem> All()
    {
        return _tasks.Values.OrderBy(task => task.Title).ToList();
    }
}
