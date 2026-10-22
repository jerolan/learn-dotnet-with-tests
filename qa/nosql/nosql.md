# NoSQL with MongoDB

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/nosql)

A relational table forces every row into the same columns. A document store lets one task carry a checklist, another carry a due date, and a third carry neither. That flexibility has a price: the database enforces fewer rules, and sorting, indexing and updates behave differently from SQL.

This appendix stores tasks in MongoDB, runs a real server in Docker, and tests the repository against it. The failure that drives the last change is a sort that puts `Beta` before `alpha`.

## The document

`src/TaskTracker.Mongo/TaskDocument.cs`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TaskTracker.Mongo;

public sealed class TaskDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

`[BsonId]` maps the property to `_id`. The string representation stores the GUID as a readable string instead of the driver's binary subtype. Pick one representation and keep it: a database written with binary GUIDs will not match a filter built from strings.

Mongo stores `DateTime` as UTC milliseconds. A local time is converted on write and comes back as UTC.

## The repository

`src/TaskTracker.Mongo/TaskRepository.cs`:

```csharp
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
```

`MatchedCount` answers "did a document match the filter". `ModifiedCount` answers "did the update change a value". Marking an already-done task as done matches but does not modify; treating that as a missing task would be wrong.

## Start Mongo once

`tests/TaskTracker.Mongo.Tests/TaskRepositoryTests.cs`:

```csharp
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
```

The container starts once per test class. `TestInitialize` drops the database and creates a new repository, so each test begins with an empty collection:

```csharp
var client = new MongoClient(_mongo.GetConnectionString());
await client.DropDatabaseAsync("tasktracker");

_repository = new TaskRepository(client.GetDatabase("tasktracker"));
```

The image is 1.1 GB, so the first run pays for the pull. Later runs reuse it.

## Write the test first

```csharp
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
```

The test says what the API promises: the fields survive a round trip and the timestamp stays UTC. The other tests cover the update, the missing task and the list.

## The sort that fails

Insert `Beta` and `alpha`, then ask for the list sorted by title:

```text
CollectionAssert.AreEqual failed. (Element at index 0 do not match.)
```

Mongo's default binary collation sorts by code point. Uppercase `B` (66) comes before lowercase `a` (97), so the order is `Beta`, `alpha`. A user reading a task list expects case-insensitive order.

Do not fix this by fetching everything and sorting in memory. The database is where the order should happen; a collection with a million tasks should not cross the network to be sorted.

Give the query a collation:

```csharp
using var cursor = await _tasks.FindAsync(
    FilterDefinition<TaskDocument>.Empty,
    new FindOptions<TaskDocument>
    {
        Sort = Builders<TaskDocument>.Sort.Ascending(task => task.Title),
        Collation = new Collation("en", strength: CollationStrength.Secondary)
    },
    cancellationToken);

return await cursor.ToListAsync(cancellationToken);
```

`Secondary` strength compares letters and ignores case. Run the tests:

```text
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 6 s
```

## What Mongo changes

- There is no schema migration for a new optional field. There is still a data migration when a field changes meaning or representation.
- A document can embed a list that SQL would put in a second table. Embed what is read together; reference what grows without bound.
- Indexes are not automatic. A query that sorts on `Title` without an index does a collection scan; add the index in a startup migration or an infrastructure script.
- Multi-document transactions need a replica set. A single-node container is fine for this repository test, not for code that depends on transactions.
- The driver serializes by property name. Renaming a C# property is a data migration unless you map the old name explicitly.

## Refactor for production

- Create indexes for the filters and sorts you actually use, and test the query plan with `explain`.
- Keep the connection string in configuration or a secret store; never in the repository.
- Enable retryable writes and choose read concern deliberately. A read from a secondary can be stale.
- Set a server selection timeout and a socket timeout. The defaults can make a failed node look like a hang.
- Back up before changing the representation of an existing field. A mixed collection is harder to fix than an empty one.
- Add health checks that ping the database, not just the process.

## What is deferred

There is no authentication, TLS, replica set, transaction, change stream or index migration here. A hands-on lab would enable auth on the container, run a three-node replica set, insert a document in a transaction, and watch a change stream deliver the insert to a background service.

## Wrapping up

What we have covered:

- A document model with an explicit BSON id representation.
- A repository for insert, read, update and sorted list.
- A real MongoDB container started once per test class.
- The case-sensitive sort failure and the collation that fixes it.
- Matched versus modified counts, indexes, transactions and representation changes as production concerns.

Next: streaming.
