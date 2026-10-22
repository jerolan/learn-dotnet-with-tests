namespace DependencyInjection;

public interface IMessageSender
{
    void Send(string message);
}

public sealed class Notifier
{
    private readonly IEnumerable<IMessageSender> _senders;

    public Notifier(IEnumerable<IMessageSender> senders)
    {
        _senders = senders;
    }

    public void Notify(string message)
    {
        var alert = $"[Alert] {message}";

        foreach (var sender in _senders)
        {
            sender.Send(alert);
        }
    }
}

