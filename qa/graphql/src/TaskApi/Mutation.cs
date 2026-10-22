using HotChocolate;

namespace TaskApi;

public sealed class Mutation
{
    public TaskItem CreateTask(string title, [Service] TaskStore store)
    {
        return store.Add(title);
    }
}
