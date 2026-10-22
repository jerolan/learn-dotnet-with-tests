# Trabajar sin mocks

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/testing/working-without-mocks)

Un mock registra llamadas y deja que el test las verifique. Un fake es una implementación pequeña que funciona. Cuando el colaborador es una interfaz tuya y el comportamiento es simple, el fake suele ser más corto, más claro y menos acoplado a la forma de las llamadas.

Vamos a probar un servicio de tareas con un repositorio en memoria y sin biblioteca de mocking. La solución terminada está en `testing/working-without-mocks`.

## Escribe primero el test

```csharp
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
```

El test usa el fake como colaborador y comprueba el estado resultante.

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'TodoService' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
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
    }

    public IReadOnlyList<Todo> List()
    {
        return Array.Empty<Todo>();
    }
}
```

El test compila y falla con `Expected:<1>. Actual:<0>`.

## Escribe el código suficiente para que pase

```csharp
public void Add(int id, string title)
{
    _repository.Add(new Todo(id, title));
}

public IReadOnlyList<Todo> List()
{
    return _repository.All();
}
```

Verde. El fake guarda la tarea en una lista, y `List` la devuelve. Sin mock, sin setup y sin verificar el orden de las llamadas.

## El fake

```csharp
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
```

Es una implementación real con el almacenamiento mínimo que funciona. El test puede inspeccionar `All()` después de la acción, que es exactamente el comportamiento que le importa a quien llama.

## Repite con requisitos nuevos

### Rechazar un título vacío

Escribe el test:

```csharp
[TestMethod]
public void Add_WithBlankTitle_ThrowsAndStoresNothing()
{
    var repository = new InMemoryTodoRepository();
    var service = new TodoService(repository);

    Assert.ThrowsException<ArgumentException>(() => service.Add(1, " "));

    Assert.AreEqual(0, repository.All().Count);
}
```

Falla porque el servicio guarda el título vacío. Agrega el guard antes de llamar al repositorio:

```csharp
if (string.IsNullOrWhiteSpace(title))
{
    throw new ArgumentException("Title is required.", nameof(title));
}

_repository.Add(new Todo(id, title));
```

Verde. La segunda aserción es la importante: una tarea rechazada no debe llegar al repositorio. El fake hace visible ese estado sin una llamada a `Verify`.

## Refactor

El código de producción es pequeño. La pregunta de diseño es cuándo usar un fake y cuándo un mock.

Usa un fake cuando:

- La interfaz es tuya y puedes escribir una implementación pequeña.
- Al test le importa el estado resultante.
- El colaborador tiene un comportamiento fácil de modelar en memoria, como un repositorio, una caché o un reloj.

Usa un mock cuando:

- La interacción en sí es el contrato, como "manda exactamente un correo" o "llama una vez a la pasarela de pago".
- El colaborador es un borde externo con una API grande que no quieres implementar.
- Necesitas simular un fallo específico de una dependencia sin construir un fake completo.

El fake vive en el proyecto de tests y puede reutilizarse en muchos tests. Si se vuelve complejo, es una señal: o la interfaz hace demasiado o el comportamiento pertenece a un adaptador real con sus propios tests.

Mocks y fakes responden preguntas distintas. Un mock dice "¿llamaste a esto, con estos argumentos?". Un fake dice "dado este comportamiento, ¿qué produjo el sistema?". La mayoría de los tests se leen mejor cuando hacen la segunda pregunta.

## Para cerrar

Lo que cubrimos:

- Un fake en memoria escrito a mano para una interfaz de repositorio.
- Tests que afirman el estado resultante en vez de la coreografía de llamadas.
- Un guard que rechaza entrada inválida antes de llegar al repositorio.
- Cuándo encaja un fake y cuándo la interacción es realmente el contrato.

El capítulo sigue [Working without mocks de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/working-without-mocks): una implementación real pequeña mantiene el test cerca del comportamiento. El siguiente capítulo convierte los hábitos de refactor en una lista de comprobación.

Siguiente: [Lista de comprobación para refactorizar](../refactoring-checklist/refactoring-checklist.es.md).

