using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace TaskTracker.Api;

public sealed class CachingTaskReader : ITaskReader
{
    private const string AllKey = "tasks:all";

    private readonly ITaskReader _inner;
    private readonly IMemoryCache _cache;
    private readonly IOptions<CacheOptions> _options;

    public CachingTaskReader(
        ITaskReader inner,
        IMemoryCache cache,
        IOptions<CacheOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public IReadOnlyList<TaskItem> All()
    {
        return _cache.GetOrCreate(AllKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow =
                TimeSpan.FromSeconds(_options.Value.Seconds);

            return _inner.All();
        })!;
    }

    public TaskItem? Find(int id)
    {
        var key = $"tasks:{id}";

        if (_cache.TryGetValue(key, out TaskItem? cached))
        {
            return cached;
        }

        var task = _inner.Find(id);

        if (task is not null)
        {
            _cache.Set(key, task, TimeSpan.FromSeconds(_options.Value.Seconds));
        }

        return task;
    }
}

