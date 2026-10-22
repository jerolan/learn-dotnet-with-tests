using HotChocolate;

namespace TaskApi;

public sealed class Query
{
    public IReadOnlyList<TaskItem> GetTasks([Service] TaskStore store)
    {
        return store.All();
    }

    public TaskItem? GetTask(Guid id, [Service] TaskStore store)
    {
        return store.Find(id);
    }
}
