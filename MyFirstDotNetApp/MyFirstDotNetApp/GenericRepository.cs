public class GenericRepository<T>
{
    private readonly List<T> _items = new List<T>();

    public void Add(T item) => _items.Add(item);

    public T GetById(int id) => _items[id];
}
