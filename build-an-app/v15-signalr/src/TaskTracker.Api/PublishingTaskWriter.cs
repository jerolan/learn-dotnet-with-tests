namespace TaskTracker.Api;

public sealed class PublishingTaskWriter : ITaskWriter
{
    private readonly ITaskWriter _inner;
    private readonly TaskEventQueue _queue;

    public PublishingTaskWriter(ITaskWriter inner, TaskEventQueue queue)
    {
        _inner = inner;
        _queue = queue;
    }

    public TaskItem Add(string title)
    {
        var task = _inner.Add(title);
        _queue.TryPublish(new TaskEvent(task.Id, task.Title, "Created"));

        return task;
    }

    public TaskItem? MarkDone(int id)
    {
        var task = _inner.MarkDone(id);

        if (task is not null)
        {
            _queue.TryPublish(new TaskEvent(task.Id, task.Title, "Done"));
        }

        return task;
    }

    public int DeleteDone()
    {
        return _inner.DeleteDone();
    }
}
