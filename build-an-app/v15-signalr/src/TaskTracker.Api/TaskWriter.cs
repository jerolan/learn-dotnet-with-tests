using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Api;

public interface ITaskWriter
{
    TaskItem Add(string title);

    TaskItem? MarkDone(int id);

    int DeleteDone();
}

public sealed class EfTaskWriter : ITaskWriter
{
    private readonly TaskDbContext _db;

    public EfTaskWriter(TaskDbContext db)
    {
        _db = db;
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

    public int DeleteDone()
    {
        var done = _db.Tasks.Where(task => task.IsDone).ToList();

        _db.Tasks.RemoveRange(done);
        _db.SaveChanges();

        return done.Count;
    }
}
