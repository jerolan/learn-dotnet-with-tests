# v2: JSON, routing y una forma de error consistente

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v2-json-routing)

La versión 1 devolvía un objeto anónimo `{ error }` y solo usaba GET y POST. La versión 2 fija el contrato de la API: una forma de error con nombre, una configuración JSON que los tests pueden ver y una ruta PATCH que cambia una tarea.

La solución terminada está en `build-an-app/v2-json-routing`.

## Escribe primero el test

```csharp
[TestMethod]
public async Task MarkDone_ChangesTheTask()
{
    using var api = new Api();
    var created = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
    var task = await created.Content.ReadFromJsonAsync<TaskItem>();

    var response = await api.Client.PatchAsync($"/tasks/{task!.Id}/done", null);

    Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

    var fetched = await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{task.Id}");
    Assert.IsNotNull(fetched);
    Assert.IsTrue(fetched.IsDone);
}
```

## Intenta ejecutar el test

La ruta no existe, así que el PATCH devuelve `404 NotFound`. La aserción espera `204 NoContent` y falla.

## Escribe el código mínimo para que el test compile y mira la salida

Agrega la operación al store:

```csharp
public TaskItem? MarkDone(int id)
{
    var index = _tasks.FindIndex(task => task.Id == id);

    if (index < 0)
    {
        return null;
    }

    var task = _tasks[index] with { IsDone = true };
    _tasks[index] = task;

    return task;
}
```

Conecta la ruta:

```csharp
app.MapMethods("/tasks/{id:int}/done", new[] { HttpMethods.Patch }, (int id, InMemoryTaskStore store) =>
{
    var task = store.MarkDone(id);

    return task is null
        ? Results.NotFound(new ErrorResponse($"Task {id} was not found."))
        : Results.NoContent();
});
```

Verde. El store reemplaza el elemento por una copia con `IsDone` en true; los records son inmutables, así que `with` crea el valor actualizado.

## Cómo funciona

`MapMethods` permite unir una ruta a un método HTTP que no tiene un ayudante `Map...` dedicado. En .NET 6, `MapPatch` no está disponible, así que `MapMethods` con `HttpMethods.Patch` es la forma explícita.

`204 NoContent` dice que la operación salió bien y no hay cuerpo que leer. Un `200` con la tarea actualizada también sería válido; la elección pertenece al contrato de la API y el test la fija.

La restricción `{id:int}` evita que la ruta matchee `/tasks/abc`. La ruta PATCH agrega un segundo segmento, `/done`, así el camino es `/tasks/{id}/done`.

## Repite con requisitos nuevos

### Una forma de error con nombre

Reemplaza el objeto de error anónimo por un record:

```csharp
public sealed record ErrorResponse(string Error);
```

Úsalo en el guard del POST y en las ramas 404 de GET/PATCH. Agrega los tests:

```csharp
[TestMethod]
public async Task UnknownTask_ReturnsAnErrorBody()
{
    using var api = new Api();

    var response = await api.Client.GetAsync("/tasks/99");

    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.IsNotNull(error);
    Assert.AreEqual("Task 99 was not found.", error.Error);
}

[TestMethod]
public async Task BlankTitle_ReturnsAnErrorBody()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest(" "));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.IsNotNull(error);
    Assert.AreEqual("Title is required.", error.Error);
}
```

Verde. Cada error que produce la API tiene ahora la misma forma. Un cliente puede parsear `error` sin adivinar si el cuerpo es un objeto, una cadena o está vacío.

### Nombres JSON

Escribe el test:

```csharp
[TestMethod]
public async Task Response_UsesCamelCaseJson()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
    var json = await response.Content.ReadAsStringAsync();

    StringAssert.Contains(json, "\"isDone\"");
    StringAssert.Contains(json, "\"title\"");
}
```

Pasa con los valores por defecto de web, pero haz explícita la política:

```csharp
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
```

Verde. El contrato JSON de la API ya no depende de un valor por defecto del framework que podría cambiar. `DefaultIgnoreCondition` deja las propiedades null fuera de las respuestas, lo que importa cuando el modelo de tarea crezca con campos opcionales.

## Refactor

Ahora hay cuatro tipos de API con trabajos distintos:

- `CreateTaskRequest` es la entrada.
- `TaskItem` es la tarea tal como la expone la API.
- `ErrorResponse` es la forma del fallo.
- El store es el borde de almacenamiento.

Mantenerlos separados significa que un cambio de almacenamiento no se filtra al JSON, y un cambio en la forma de error no exige un tipo de dominio nuevo. La versión 4 agrega una base de datos detrás del store; las firmas de los endpoints quedan iguales.

`ErrorResponse` es pequeño a propósito. Cuando llegue la validación en la versión 6, crecerá a una forma de errores por campo o a un documento Problem Details. El punto de esta versión es que la forma tiene nombre y test.

## Para cerrar

Lo que cubrimos:

- Una ruta PATCH con `MapMethods` y `HttpMethods.Patch`.
- `204 NoContent` y la actualización inmutable con `with` en el store.
- Un `ErrorResponse` con nombre usado por todos los caminos de fallo.
- JSON camelCase explícito y manejo de null.
- Tipos de entrada, salida, error y almacenamiento como bordes separados.

Siguiente: v3: configuración y el patrón de opciones.

