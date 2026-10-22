using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Api;

public interface ITaskReader
{
    IReadOnlyList<TaskItem> All();

    TaskItem? Find(int id);
}

public sealed class DapperTaskReader : ITaskReader
{
    private readonly TaskDbContext _db;

    public DapperTaskReader(TaskDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<TaskItem> All()
    {
        var rows = Connection()
            .Query<TaskRow>("SELECT Id, Title, IsDone FROM Tasks ORDER BY Id")
            .AsList();

        return rows.Select(row => row.ToTaskItem()).ToList();
    }

    public TaskItem? Find(int id)
    {
        var row = Connection().QuerySingleOrDefault<TaskRow>(
            "SELECT Id, Title, IsDone FROM Tasks WHERE Id = @Id",
            new { Id = id });

        return row?.ToTaskItem();
    }

    private IDbConnection Connection()
    {
        var connection = _db.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        return connection;
    }

    private sealed class TaskRow
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public long IsDone { get; set; }

        public TaskItem ToTaskItem()
        {
            return new TaskItem((int)Id, Title, IsDone != 0);
        }
    }
}
