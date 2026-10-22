# Tests de aceptación

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/testing/acceptance-tests)

Un test de aceptación comprueba una característica a través del mismo borde que usa la persona: para una API HTTP, manda peticiones y lee respuestas, no llama a un método de servicio. El test responde "qué significa ganar" antes de que decidas cómo se organiza el código por dentro.

Vamos a construir una API de tareas pequeña y probarla con `WebApplicationFactory`. La solución terminada está en `testing/acceptance-tests`; si la estás armando desde cero, crea un proyecto web en `src/TodoApi`, un proyecto de MSTest en `tests/TodoApi.Tests`, una solución, una referencia de los tests a la app y el paquete del host de tests:

```sh
dotnet new web -n TodoApi -o src/TodoApi
dotnet new mstest -n TodoApi.Tests -o tests/TodoApi.Tests
dotnet add tests/TodoApi.Tests/TodoApi.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 6.0.36
dotnet add tests/TodoApi.Tests/TodoApi.Tests.csproj reference src/TodoApi/TodoApi.csproj
dotnet new sln -n AcceptanceTests
dotnet sln add src/TodoApi/TodoApi.csproj tests/TodoApi.Tests/TodoApi.Tests.csproj
```

## Escribe primero el test

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TodoApi.Tests;

[TestClass]
public sealed class TodoApiTests
{
    [TestMethod]
    public async Task CanCreateAndListTodos()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/todos", new Todo(1, "Write tests"));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");

        Assert.IsNotNull(todos);
        Assert.AreEqual(1, todos.Count);
        Assert.AreEqual("Write tests", todos[0].Title);
    }
}
```

El test habla HTTP. No sabe si el endpoint usa una lista, un repositorio o una base de datos.

## Intenta ejecutar el test

La app arranca sin endpoints:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Run();

public sealed record Todo(int Id, string Title);

public partial class Program
{
}
```

El test compila y falla con `Assert.AreEqual failed. Expected:<Created>. Actual:<NotFound>`. La característica todavía no existe.

La declaración `public partial class Program` es lo que deja al proyecto de tests usar `WebApplicationFactory<Program>`. Las sentencias de nivel superior generan un `Program` interno; la declaración parcial lo hace visible sin agregar un `Main`.

## Escribe el código suficiente para que pase

Agrega los dos endpoints:

```csharp
var todos = new List<Todo>();

app.MapGet("/todos", () => todos);

app.MapPost("/todos", (Todo todo) =>
{
    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.Run();
```

Verde. El POST devuelve `201 Created` con la tarea, y el GET devuelve la lista como JSON. El test pasó por routing, model binding, serialización JSON y la lógica del endpoint.

## Cómo funciona

`WebApplicationFactory<Program>` arranca la aplicación en memoria. `CreateClient()` te da un `HttpClient` cuyas peticiones van al servidor en memoria, no a un puerto de tu máquina.

Debajo de la factory está `TestServer`, el servidor en memoria que maneja las peticiones. Casi nunca construyes `TestServer` directamente; la factory lo configura con el arranque de la app.

La factory usa el camino de arranque real de la app: el mismo middleware, routing y configuración JSON que producción. Eso es lo que hace que esto sea un test de aceptación y no un test unitario. Puedes refactorizar el endpoint a un servicio, un repositorio o una base de datos y el test sigue pasando mientras el comportamiento HTTP no cambie.

El paquete del host de tests debe coincidir con el framework de destino. Una app `net6.0` usa `Microsoft.AspNetCore.Mvc.Testing` `6.0.x`. Mezclar un host más nuevo con un runtime viejo es una fuente habitual de errores de arranque.

## Repite con requisitos nuevos

### Una lista vacía

Escribe el test:

```csharp
[TestMethod]
public async Task EmptyList_ReturnsNoTodos()
{
    using var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();

    var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");

    Assert.IsNotNull(todos);
    Assert.AreEqual(0, todos.Count);
}
```

Pasa. Cada test crea su propia factory, que crea su propia app y su propia lista `todos`. Si los tests compartieran una factory, la tarea del primero se filtraría al segundo.

## Refactor

Todavía no hay código de producción que limpiar. El borde importante es el test.

Los tests de aceptación son más lentos que los unitarios porque arrancan la aplicación y pasan por el stack HTTP. Resérvalos para los caminos críticos que le importan a la persona: crear una tarea, listar tareas, rechazar entrada inválida. Prueba el detalle de un cálculo o una rama en el nivel unitario.

Un test de aceptación debe describir comportamiento, no implementación. `CanCreateAndListTodos` sobrevive a un cambio de lista a base de datos. Un test que afirme que el endpoint llamó a un método del repositorio se rompería con ese refactor, y estaría probando el interior en vez de la característica.

La factory también puede reemplazar servicios para escenarios de integración: una base de datos de test, un emisor de correo falso, un reloj. Esa es la costura para usar cuando la característica necesita infraestructura. La Parte 3 la usa para pruebas contra base de datos.

## Para cerrar

Lo que cubrimos:

- Un test de aceptación por HTTP con `WebApplicationFactory<Program>`.
- Arrancar la app real en memoria y crear un `HttpClient`.
- `PostAsJsonAsync`, `GetFromJsonAsync` y códigos de estado.
- La declaración `public partial class Program` que necesita el host de tests.
- Una factory por test para aislar el estado de la aplicación.
- La diferencia entre un test de aceptación y uno unitario.

El capítulo sigue la [Introducción a los tests de aceptación de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/intro-to-acceptance-tests): escribe un test para la característica que la persona puede ver y después construye hacia adentro hasta que pase. .NET provee el host en memoria a través de `WebApplicationFactory`.

Siguiente: [Escalar tests de aceptación](../scaling-acceptance-tests/scaling-acceptance-tests.es.md).

