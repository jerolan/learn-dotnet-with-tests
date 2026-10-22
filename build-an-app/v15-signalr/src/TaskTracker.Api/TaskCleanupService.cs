using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TaskTracker.Api;

public sealed class CleanupOptions
{
    public const string SectionName = "Cleanup";

    public int IntervalSeconds { get; set; } = 60;
}

public sealed class TaskCleanupJob
{
    private readonly ITaskWriter _writer;
    private readonly IAuditLog _audit;

    public TaskCleanupJob(ITaskWriter writer, IAuditLog audit)
    {
        _writer = writer;
        _audit = audit;
    }

    public int Run()
    {
        var deleted = _writer.DeleteDone();
        _audit.Record($"Cleanup:{deleted}");

        return deleted;
    }
}

public sealed class TaskCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<CleanupOptions> _options;
    private readonly ILogger<TaskCleanupService> _logger;

    public TaskCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<CleanupOptions> options,
        ILogger<TaskCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var job = scope.ServiceProvider.GetRequiredService<TaskCleanupJob>();
                    var deleted = job.Run();

                    _logger.LogInformation(
                        "Cleanup removed {Count} completed tasks",
                        deleted);
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.Value.IntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
