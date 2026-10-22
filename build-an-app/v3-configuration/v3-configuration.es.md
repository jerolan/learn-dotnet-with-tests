# v3: Configuración y el patrón de opciones

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v3-configuration)

La versión 2 validaba que el título no estuviera vacío. La versión 3 agrega una regla real: una longitud máxima de título que viene de configuración. El handler lee un objeto de opciones tipado, y los tests sobrescriben el valor sin editar un archivo.

La solución terminada está en `build-an-app/v3-configuration`.

## Cómo funciona

`appsettings.json` proporciona el límite por defecto:

```json
{
  "Tasks": {
    "MaxTitleLength": 120
  }
}
```

`builder.Configuration.GetSection("Tasks")` selecciona ese subárbol. `Configure<TaskOptions>` enlaza sus claves con las propiedades y registra `IOptions<TaskOptions>` en DI. El endpoint pide `IOptions<TaskOptions>` y lee `Value`; no parsea una cadena ni entra a `IConfiguration`.

`IOptions<T>` es una foto singleton que se lee una vez. Si el valor debe cambiar mientras la app corre, usa `IOptionsMonitor<T>` o `IOptionsSnapshot<T>`; para esta regla de arranque, el singleton es el valor por defecto correcto.

## Cómo probarlo

Envía un título por la API HTTP y comprueba el estado y el cuerpo del error. El driver puede sobrescribir el límite para un host sin editar los archivos de producción:

```csharp
public Api(int? maxTitleLength = null)
{
    var factory = new WebApplicationFactory<Program>();

    if (maxTitleLength.HasValue)
    {
        factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tasks:MaxTitleLength"] = maxTitleLength.Value.ToString()
                })));
    }

    _factory = factory;
    Client = _factory.CreateClient();
}
```

El `WithWebHostBuilder` crea un host modificado para ese test. `ConfigureAppConfiguration` agrega la fuente en memoria después de los archivos de la app, así gana el valor del test y los archivos de producción quedan intactos.

## Escribe primero el test

```csharp
[TestMethod]
public async Task TitleLongerThanTheConfiguredLimit_ReturnsBadRequest()
{
    using var api = new Api(maxTitleLength: 10);

    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest(new string('a', 11)));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.IsNotNull(error);
    StringAssert.Contains(error.Error, "10");
}
```

## Intenta ejecutar el test

La API todavía no tiene regla de longitud, así que el POST devuelve `201 Created` y el test falla con `Expected:<BadRequest>. Actual:<Created>`.

## Escribe el código mínimo para que el test compile y mira la salida

Agrega y enlaza el tipo de opciones:

```csharp
public sealed class TaskOptions
{
    public const string SectionName = "Tasks";

    public int MaxTitleLength { get; set; } = 120;
}
```

```csharp
builder.Services.Configure<TaskOptions>(
    builder.Configuration.GetSection(TaskOptions.SectionName));
```

La app todavía acepta el título de 11 caracteres. El ajuste ya está registrado, pero el handler todavía no lo usa.

## Escribe el código suficiente para que pase

Usa las opciones tipadas en el handler POST:

```csharp
app.MapPost("/tasks", (CreateTaskRequest request, InMemoryTaskStore store, IOptions<TaskOptions> options) =>
{
    var maxTitleLength = options.Value.MaxTitleLength;

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new ErrorResponse("Title is required."));
    }

    var title = request.Title.Trim();

    if (title.Length > maxTitleLength)
    {
        return Results.BadRequest(new ErrorResponse($"Title must be at most {maxTitleLength} characters."));
    }

    var task = store.Add(title);

    return Results.Created($"/tasks/{task.Id}", task);
});
```

Verde. El título de 11 caracteres se rechaza con el límite configurado en el mensaje.

## Refactor

El tipo de opciones es pequeño, pero dos hábitos mantienen sana la configuración:

- Valida las opciones al arrancar. `MaxTitleLength` debe ser positivo; un cero o un negativo rechazaría todos los títulos. Las anotaciones de datos y `ValidateOnStart()` convierten eso en un fallo de arranque en vez de un 400 raro en cada petición.
- Mantén el acceso a configuración en la raíz de composición. Un handler que lee `configuration["Tasks:MaxTitleLength"]` esconde una dependencia y es más difícil de testear. El endpoint pide las opciones tipadas que necesita.

Para valores por entorno, agrega `appsettings.Development.json` o usa variables de entorno con las mismas claves. La fuente en memoria del test es otro proveedor de configuración, y por eso pisa el archivo limpiamente.

## Repite con requisitos nuevos

### El valor del borde

Escribe el test:

```csharp
[TestMethod]
public async Task TitleAtTheConfiguredLimit_Succeeds()
{
    using var api = new Api(maxTitleLength: 10);

    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest(new string('a', 10)));

    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
}
```

Pasa. La regla es "más que el límite falla", así que exactamente el límite se permite. El test hace explícito ese borde.

### El valor por defecto de appsettings

Escribe el test:

```csharp
[TestMethod]
public async Task DefaultLimit_ComesFromAppSettings()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest(new string('a', 121)));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.IsNotNull(error);
    StringAssert.Contains(error.Error, "120");
}
```

Pasa porque el host de test lee el `appsettings.json` de la app. Este es el test que prueba que el valor por defecto está cableado, no solo el mecanismo de override.

## Para cerrar

Lo que cubrimos:

- Una clase de opciones tipada enlazada a una sección de configuración.
- `IOptions<T>` inyectado en un handler de API mínima.
- Un host de test que sobrescribe configuración con `WithWebHostBuilder` y `AddInMemoryCollection`.
- Valores por defecto desde `appsettings.json` y un test de borde.
- Validación y elección de tiempo de vida para las opciones.

Siguiente: [v4: EF Core, un modelo code-first y una migración](../v4-ef-core/v4-ef-core.es.md).

