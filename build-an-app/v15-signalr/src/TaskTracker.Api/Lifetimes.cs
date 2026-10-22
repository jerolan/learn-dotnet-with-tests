namespace TaskTracker.Api;

public sealed class RequestContext
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class AppInstance
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class ReadCounter
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Increment()
    {
        Interlocked.Increment(ref _count);
    }
}
