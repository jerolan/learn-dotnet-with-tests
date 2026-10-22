namespace Generics;

public interface IEntity
{
    int Id { get; }
}

public sealed record Product(int Id, string Name) : IEntity;

public sealed record Customer(int Id, string Name) : IEntity;

public sealed class Repository<T> where T : class, IEntity
{
    private readonly Dictionary<int, T> _items = new();

    public void Add(T item)
    {
        _items[item.Id] = item;
    }

    public T? Get(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    public IReadOnlyCollection<T> All => _items.Values;
}

