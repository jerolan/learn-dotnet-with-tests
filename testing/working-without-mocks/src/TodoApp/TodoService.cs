namespace TodoApp;

public sealed record Todo(int Id, string Title);

public interface ITodoRepository
{
    void Add(Todo todo);

    IReadOnlyList<Todo> All();
}

public sealed class TodoService
{
    private readonly ITodoRepository _repository;

    public TodoService(ITodoRepository repository)
    {
        _repository = repository;
    }

    public void Add(int id, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        _repository.Add(new Todo(id, title));
    }

    public IReadOnlyList<Todo> List()
    {
        return _repository.All();
    }
}

