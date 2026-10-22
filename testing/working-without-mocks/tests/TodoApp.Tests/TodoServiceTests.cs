using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TodoApp.Tests;

[TestClass]
public sealed class TodoServiceTests
{
    [TestMethod]
    public void Add_StoresTheTodo()
    {
        var repository = new InMemoryTodoRepository();
        var service = new TodoService(repository);

        service.Add(1, "Write tests");

        var todos = service.List();

        Assert.AreEqual(1, todos.Count);
        Assert.AreEqual("Write tests", todos[0].Title);
    }

    [TestMethod]
    public void Add_WithBlankTitle_ThrowsAndStoresNothing()
    {
        var repository = new InMemoryTodoRepository();
        var service = new TodoService(repository);

        Assert.ThrowsException<ArgumentException>(() => service.Add(1, " "));

        Assert.AreEqual(0, repository.All().Count);
    }

    private sealed class InMemoryTodoRepository : ITodoRepository
    {
        private readonly List<Todo> _todos = new();

        public void Add(Todo todo)
        {
            _todos.Add(todo);
        }

        public IReadOnlyList<Todo> All()
        {
            return _todos;
        }
    }
}

