namespace TaskTracker.Api;

public sealed class CountingTaskReader : ITaskReader
{
    private readonly ITaskReader _inner;
    private readonly ReadCounter _counter;

    public CountingTaskReader(ITaskReader inner, ReadCounter counter)
    {
        _inner = inner;
        _counter = counter;
    }

    public IReadOnlyList<TaskItem> All()
    {
        _counter.Increment();
        return _inner.All();
    }

    public TaskItem? Find(int id)
    {
        _counter.Increment();
        return _inner.Find(id);
    }
}

