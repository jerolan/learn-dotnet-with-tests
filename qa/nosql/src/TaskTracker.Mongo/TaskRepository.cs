using MongoDB.Driver;

namespace TaskTracker.Mongo;

public sealed class TaskRepository
{
    private readonly IMongoCollection<TaskDocument> _tasks;

    public TaskRepository(IMongoDatabase database)
    {
        _tasks = database.GetCollection<TaskDocument>("tasks");
    }

    public Task InsertAsync(TaskDocument task, CancellationToken cancellationToken = default)
    {
        return _tasks.InsertOneAsync(task, cancellationToken: cancellationToken);
    }

    public async Task<TaskDocument?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _tasks
            .Find(task => task.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskDocument>> ListByTitleAsync(
        CancellationToken cancellationToken = default)
    {
        using var cursor = await _tasks.FindAsync(
            FilterDefinition<TaskDocument>.Empty,
            new FindOptions<TaskDocument>
            {
                Sort = Builders<TaskDocument>.Sort.Ascending(task => task.Title),
                Collation = new Collation("en", strength: CollationStrength.Secondary)
            },
            cancellationToken);

        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkDoneAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _tasks.UpdateOneAsync(
            task => task.Id == id,
            Builders<TaskDocument>.Update.Set(task => task.IsDone, true),
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }
}
