# v2: JSON, routing y una forma de error consistente

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v2-json-routing)

La versión 1 devolvía un objeto anónimo `{ error }` y solo usaba GET y POST. La versión 2 fija el contrato de la API: una forma de error con nombre, una configuración JSON que los tests pueden ver y una ruta PATCH que cambia una tarea.

La solución terminada está en `build-an-app/v2-json-routing`.

## Cómo funciona

`MapMethods` enlaza una ruta con un verbo HTTP específico. .NET 6 no tiene un ayudante `MapPatch` dedicado, así que se usa `MapMethods` con `HttpMethods.Patch`. La restricción entera evita que `/tasks/abc` coincida, y el segmento `/done` forma la ruta `/tasks/{id}/done`. `204 NoContent` indica que la operación salió bien y no hay cuerpo; un `200` con la tarea actualizada también serviría, pero el test fija el contrato elegido. Esta versión también nombra la respuesta de error y hace explícito el formato JSON.

## Cómo probarlo

Envía PATCH y GET por el `HttpClient` que crea `WebApplicationFactory`. Comprueba el estado de la respuesta y lee la tarea de vuelta; así pruebas el contrato público sin depender de la implementación del store.

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

El test sigue fallando con `NotFound`; el store ya puede actualizar una tarea, pero la API todavía no tiene una ruta PATCH.

## Escribe el código suficiente para que pase

Conecta la ruta:

```csharp
app.MapMethods("/tasks/{id:int}/done", new[] { HttpMethods.Patch }, (int id, InMemoryTaskStore store) =>
{
    var task = store.MarkDone(id);

    return task is null
        ? Results.NotFound()
        : Results.NoContent();
});
```

Verde. El store reemplaza el elemento por una copia con `IsDone` en true; los records son inmutables, así que `with` crea el valor actualizado.

## Refactor

Los tipos del contrato tienen trabajos distintos: el request es la entrada, `TaskItem` representa la tarea en la API y el store maneja la persistencia. Al conservar estos bordes, los cambios de almacenamiento no se filtran al JSON. La versión 4 agrega una base de datos detrás del store y mantiene iguales las firmas de los endpoints.

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

`ErrorResponse` es pequeño a propósito. Cuando llegue la validación en la versión 6, crecerá a una forma de errores por campo o a un documento Problem Details. Esta versión hace que la forma del fallo tenga nombre y pueda probarse.

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

## Para cerrar

Lo que cubrimos:

- Una ruta PATCH con `MapMethods` y `HttpMethods.Patch`.
- `204 NoContent` y la actualización inmutable con `with` en el store.
- Un `ErrorResponse` con nombre usado por todos los caminos de fallo.
- JSON camelCase explícito y manejo de null.
- Tipos de entrada, salida, error y almacenamiento como bordes separados.

Siguiente: [v3: configuración y el patrón de opciones](../v3-configuration/v3-configuration.es.md).

