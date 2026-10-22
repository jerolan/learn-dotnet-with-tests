using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Api;

public interface ITaskRepository
{
    IReadOnlyList<TaskItem> All();

    TaskItem? Find(int id);

    TaskItem Add(string title);

    TaskItem? MarkDone(int id);
}

public sealed class EfTaskRepository : ITaskRepository
{
    private readonly TaskDbContext _db;

    public EfTaskRepository(TaskDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<TaskItem> All()
    {
        return _db.Tasks
            .AsNoTracking()
            .OrderBy(task => task.Id)
            .Select(task => new TaskItem(task.Id, task.Title, task.IsDone))
            .ToList();
    }

    public TaskItem? Find(int id)
    {
        var task = _db.Tasks.AsNoTracking().SingleOrDefault(item => item.Id == id);

        return task is null ? null : new TaskItem(task.Id, task.Title, task.IsDone);
    }

    public TaskItem Add(string title)
    {
        var task = new TaskEntity { Title = title };
        _db.Tasks.Add(task);
        _db.SaveChanges();

        return new TaskItem(task.Id, task.Title, task.IsDone);
    }

    public TaskItem? MarkDone(int id)
    {
        var task = _db.Tasks.SingleOrDefault(item => item.Id == id);

        if (task is null)
        {
            return null;
        }

        task.IsDone = true;
        _db.SaveChanges();

        return new TaskItem(task.Id, task.Title, task.IsDone);
    }
}

