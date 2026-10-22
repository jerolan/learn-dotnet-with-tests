using Microsoft.Extensions.Caching.Memory;

namespace TaskTracker.Api;

public sealed class InvalidatingTaskWriter : ITaskWriter
{
    private const string AllKey = "tasks:all";

    private readonly ITaskWriter _inner;
    private readonly IMemoryCache _cache;

    public InvalidatingTaskWriter(ITaskWriter inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public TaskItem Add(string title)
    {
        var task = _inner.Add(title);
        _cache.Remove(AllKey);

        return task;
    }

    public TaskItem? MarkDone(int id)
    {
        var task = _inner.MarkDone(id);
        _cache.Remove(AllKey);
        _cache.Remove($"tasks:{id}");

        return task;
    }

    public int DeleteDone()
    {
        var deleted = _inner.DeleteDone();
        _cache.Remove(AllKey);

        return deleted;
    }
}
