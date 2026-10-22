namespace TaskTracker.Api;

public sealed record TaskItem(int Id, string Title, bool IsDone);

public sealed class InMemoryTaskStore
{
    private readonly List<TaskItem> _tasks = new();
    private int _nextId = 1;

    public IReadOnlyList<TaskItem> All()
    {
        return _tasks;
    }

    public TaskItem? Find(int id)
    {
        return _tasks.FirstOrDefault(task => task.Id == id);
    }

    public TaskItem Add(string title)
    {
        var task = new TaskItem(_nextId++, title, false);
        _tasks.Add(task);
        return task;
    }
}

