# v9: Logging estructurado

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v9-logging)

Una línea que dice "tarea creada" es difícil de consultar. Un evento con un campo `TaskId` y un campo `Title` es un dato. La versión 9 agrega eventos estructurados, un scope de correlación y un logger de test que captura los campos en vez de comparar texto formateado.

La solución terminada está en `build-an-app/v9-logging`.

## Escribe primero el test

```csharp
[TestMethod]
public async Task CreateTask_LogsTaskIdAndTitle()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest("Write tests"));

    var task = await response.Content.ReadFromJsonAsync<TaskItem>();
    var record = api.Logs.Single(entry => entry.Message.Contains("created"));

    Assert.AreEqual(task!.Id, record.Fields["TaskId"]);
    Assert.AreEqual("Write tests", record.Fields["Title"]);
}
```

Contra v8 no hay evento de log, así que el test no encuentra ningún registro. El driver instala un proveedor que graba:

```csharp
builder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddProvider(_logs);
    logging.SetMinimumLevel(LogLevel.Information);
});
```

El método `Log` del proveedor recibe el estado del mensaje como pares clave/valor. Esos son los datos estructurados que lee el test.

## Agrega el evento

```csharp
app.MapPost("/tasks", async (
    CreateTaskRequest request,
    ITaskWriter writer,
    IValidator<CreateTaskRequest> validator,
    ILogger<Program> logger) =>
{
    // validación ...

    var task = writer.Add(request.Title.Trim());

    logger.LogInformation(
        "Task {TaskId} created with title {Title}",
        task.Id,
        task.Title);

    return Results.Created($"/tasks/{task.Id}", task);
});
```

Verde. La plantilla nombra los campos; los argumentos los llenan. Un proveedor puede guardar `TaskId=1` y `Title=Write tests` sin parsear el texto renderizado.

No armes el mensaje con interpolación:

```csharp
logger.LogInformation($"Task {task.Id} created"); // pierde los campos
```

La cadena interpolada es un mensaje opaco. La versión con plantilla mantiene nombres y valores separados.

## Cómo funciona

El pipeline de logging tiene tres piezas: el lugar de la llamada, una categoría `ILogger<T>` y un proveedor. La categoría suele ser el tipo dueño del log, así `ILogger<Program>` para el endpoint y `ILogger<CorrelationIdMiddleware>` para el middleware.

Una plantilla usa llaves para los campos con nombre. El proveedor recibe la plantilla y los valores. Los proveedores de consola y JSON los renderizan; un proveedor de test puede inspeccionarlos; un exportador de OpenTelemetry puede adjuntarlos a un span.

Los niveles acotan la salida. `Information` es para eventos de negocio normales, `Warning` para algo inesperado pero manejado, `Error` para un fallo. Evita loguear la entrada de cada método; loguea las decisiones y los bordes.

## Repite con requisitos nuevos

### Un id de correlación

Escribe el test:

```csharp
[TestMethod]
public async Task CorrelationId_FlowsIntoTheResponseAndTheScope()
{
    using var api = new Api();
    var request = new HttpRequestMessage(HttpMethod.Get, "/tasks");
    request.Headers.Add("X-Correlation-Id", "test-123");

    var response = await api.Client.SendAsync(request);

    Assert.AreEqual("test-123", response.Headers.GetValues("X-Correlation-Id").Single());

    var record = api.Logs.Last(entry => entry.Message.Contains("Handled"));
    Assert.AreEqual("test-123", record.Fields["CorrelationId"]);
}
```

Agrega el middleware:

```csharp
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value)
            && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            logger.LogInformation(
                "Handled {Method} {Path}",
                context.Request.Method,
                context.Request.Path.Value);

            await _next(context);
        }
    }
}
```

Regístralo:

```csharp
app.UseMiddleware<CorrelationIdMiddleware>();
```

Verde. La respuesta lleva el id y el evento dentro del scope lleva `CorrelationId` como campo.

El scope aplica a cada log escrito a través de ese logger durante la petición. En una app más grande, el id de correlación también viaja en cabeceras HTTP salientes y metadatos de mensajes, así un id sigue una petición entre servicios.

## Refactor

Algunos hábitos mantienen útil la salida:

- Loguea en el borde: petición recibida, tarea creada, dependencia fallida. Saltea el bucle interno.
- Usa plantillas estables. Cambiar `Task {TaskId} created` rompe los tableros que consultan el evento; agrega un evento nuevo en vez de renombrar el viejo.
- Nunca loguees secretos, tokens, contraseñas ni cuerpos completos. Loguea identificadores y resultados.
- Pon el contexto compartido en scopes en vez de repetirlo en cada mensaje.
- Mantén la configuración de proveedores en la raíz de composición; el dominio pide `ILogger<T>` y no sabe adónde van los logs.

El proveedor que graba en el proyecto de tests es un ejemplo pequeño. Un proveedor de producción escribe JSON, lo envía a un colector o ambas cosas; el lugar de la llamada no cambia.

## Para cerrar

Lo que cubrimos:

- Plantillas estructuradas con campos con nombre.
- Categorías `ILogger<T>` y el pipeline de logging.
- Un proveedor que captura campos en los tests.
- Una cabecera de correlación y un scope de logging.
- Niveles de log y el hábito de loguear en los bordes.
- Evitar interpolación, secretos y logs ruidosos por línea.

Siguiente: v10: caché con IMemoryCache.

