using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace TaskTracker.Mongo.Tests;

[TestClass]
public sealed class TaskRepositoryTests
{
    private static MongoDbContainer _mongo = null!;
    private TaskRepository _repository = null!;

    [ClassInitialize]
    public static async Task StartMongo(TestContext context)
    {
        _mongo = new MongoDbBuilder()
            .WithImage("mongo:6.0")
            .Build();

        await _mongo.StartAsync();
    }

    [ClassCleanup]
    public static async Task StopMongo()
    {
        await _mongo.DisposeAsync();
    }

    [TestInitialize]
    public async Task ResetDatabase()
    {
        var client = new MongoClient(_mongo.GetConnectionString());
        await client.DropDatabaseAsync("tasktracker");

        _repository = new TaskRepository(client.GetDatabase("tasktracker"));
    }

    [TestMethod]
    public async Task InsertedTaskComesBackWithTheSameFields()
    {
        var task = new TaskDocument
        {
            Id = Guid.NewGuid(),
            Title = "Write the chapter",
            CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };

        await _repository.InsertAsync(task);

        var found = await _repository.GetAsync(task.Id);

        Assert.IsNotNull(found);
        Assert.AreEqual(task.Title, found!.Title);
        Assert.IsFalse(found.IsDone);
        Assert.AreEqual(DateTimeKind.Utc, found.CreatedAt.Kind);
    }

    [TestMethod]
    public async Task MarkDoneUpdatesTheStoredDocument()
    {
        var task = new TaskDocument
        {
            Id = Guid.NewGuid(),
            Title = "Write the chapter",
            CreatedAt = DateTime.UtcNow
        };
        await _repository.InsertAsync(task);

        var changed = await _repository.MarkDoneAsync(task.Id);

        Assert.IsTrue(changed);
        var found = await _repository.GetAsync(task.Id);
        Assert.IsTrue(found!.IsDone);
    }

    [TestMethod]
    public async Task MarkDoneReturnsFalseForMissingTask()
    {
        var changed = await _repository.MarkDoneAsync(Guid.NewGuid());

        Assert.IsFalse(changed);
    }

    [TestMethod]
    public async Task ListByTitleUsesTheDatabaseOrder()
    {
        await _repository.InsertAsync(new TaskDocument
        {
            Id = Guid.NewGuid(),
            Title = "Zebra",
            CreatedAt = DateTime.UtcNow
        });
        await _repository.InsertAsync(new TaskDocument
        {
            Id = Guid.NewGuid(),
            Title = "Alpha",
            CreatedAt = DateTime.UtcNow
        });

        var tasks = await _repository.ListByTitleAsync();

        CollectionAssert.AreEqual(
            new[] { "Alpha", "Zebra" },
            tasks.Select(task => task.Title).ToArray());
    }

    [TestMethod]
    public async Task ListByTitleIgnoresCase()
    {
        await _repository.InsertAsync(new TaskDocument
        {
            Id = Guid.NewGuid(),
            Title = "Beta",
            CreatedAt = DateTime.UtcNow
        });
        await _repository.InsertAsync(new TaskDocument
        {
            Id = Guid.NewGuid(),
            Title = "alpha",
            CreatedAt = DateTime.UtcNow
        });

        var tasks = await _repository.ListByTitleAsync();

        CollectionAssert.AreEqual(
            new[] { "alpha", "Beta" },
            tasks.Select(task => task.Title).ToArray());
    }
}
