namespace UsingKeyword;

public sealed class Session : IDisposable
{
    private readonly List<string> _log;

    public Session(List<string> log)
    {
        _log = log;
        _log.Add("open");
    }

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        _log.Add("dispose");
    }
}

public static class SessionRunner
{
    public static void Run(List<string> log, Action<Session> action)
    {
        using var session = new Session(log);
        action(session);
    }
}

