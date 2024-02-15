namespace MyFirstDotNetApp;

public class GenericRepository<T>
{
    private readonly List<T> _items = new();

    public void Add(T item)
    {
        _items.Add(item);
    }

    public T GetById(int id)
    {
        return _items[id];
    }
}