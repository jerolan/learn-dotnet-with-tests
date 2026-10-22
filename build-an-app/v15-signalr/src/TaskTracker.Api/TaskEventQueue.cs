using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace TaskTracker.Api;

public sealed record TaskEvent(int TaskId, string Title, string Type);

public sealed class TaskEventQueue
{
    private readonly Channel<TaskEvent> _channel = Channel.CreateUnbounded<TaskEvent>();

    public bool TryPublish(TaskEvent taskEvent)
    {
        return _channel.Writer.TryWrite(taskEvent);
    }

    public IAsyncEnumerable<TaskEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}

public interface IAuditLog
{
    void Record(string entry);
}

public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyCollection<string> Entries => _entries.ToArray();

    public void Record(string entry)
    {
        _entries.Enqueue(entry);
    }
}

public sealed class TaskEventProcessor
{
    private readonly IAuditLog _audit;
    private readonly IHubContext<TaskUpdatesHub> _hub;

    public TaskEventProcessor(IAuditLog audit, IHubContext<TaskUpdatesHub> hub)
    {
        _audit = audit;
        _hub = hub;
    }

    public async Task ProcessAsync(TaskEvent taskEvent, CancellationToken cancellationToken)
    {
        _audit.Record($"{taskEvent.Type}:{taskEvent.TaskId}");
        await _hub.Clients.All.SendAsync("TaskChanged", taskEvent, cancellationToken);
    }
}

public sealed class TaskEventConsumer : BackgroundService
{
    private readonly TaskEventQueue _queue;
    private readonly TaskEventProcessor _processor;
    private readonly ILogger<TaskEventConsumer> _logger;

    public TaskEventConsumer(
        TaskEventQueue queue,
        TaskEventProcessor processor,
        ILogger<TaskEventConsumer> logger)
    {
        _queue = queue;
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var taskEvent in _queue.ReadAllAsync(stoppingToken))
            {
                await _processor.ProcessAsync(taskEvent, stoppingToken);

                _logger.LogInformation(
                    "Processed {Type} for task {TaskId}",
                    taskEvent.Type,
                    taskEvent.TaskId);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
