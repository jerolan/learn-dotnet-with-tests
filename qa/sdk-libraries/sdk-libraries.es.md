# Bibliotecas SDK

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/sdk-libraries)

Tres aplicaciones llaman a la misma API de tareas. Cada una configura una base address, elige opciones de JSON, revisa el estado y lee el cuerpo a su manera. Las diferencias son pequeñas hasta que una manda `TaskId` donde la API espera `taskId`, o se olvida de disponer una respuesta.

Un cliente tipado empaquetado como biblioteca NuGet mueve esas decisiones a un solo lugar. Este apéndice construye `TaskTracker.Client`, lo prueba sin red y lo empaqueta en un `.nupkg`.

## Cómo funciona

El paquete envuelve un `HttpClient` que configura quien lo consume. La biblioteca concentra las rutas de la API, los nombres JSON y la conversión de errores; los metadatos del paquete controlan qué recibe el consumidor.

### El proyecto

`src/TaskTracker.Client/TaskTracker.Client.csproj`:

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <PackageId>TaskTracker.Client</PackageId>
  <Version>1.0.0</Version>
  <Authors>Learn .NET with Tests</Authors>
  <Description>Typed HTTP client for the TaskTracker API.</Description>
  <PackageTags>tasks;http;client</PackageTags>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>

<ItemGroup>
  <None Include="README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

`GenerateDocumentationFile` convierte los comentarios XML en un archivo que viaja con el ensamblado, así los tooltips del IDE funcionan para quien consume. `PackageReadmeFile` pone el README en la página del paquete. Ninguno cambia el código; los dos cambian lo que recibe el consumidor.

### El cliente

`src/TaskTracker.Client/TaskClient.cs`:

```csharp
public sealed class TaskClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public TaskClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("tasks", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<TaskDto>>(
            JsonOptions,
            cancellationToken) ?? new List<TaskDto>();
    }

    public async Task<TaskDto> CreateTaskAsync(
        string title,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(
            "tasks",
            new { title },
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<TaskDto>(
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty body.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new TaskClientException(
            response.StatusCode,
            $"The API returned {(int)response.StatusCode}: {body}");
    }
}
```

El cliente recibe un `HttpClient` en vez de crearlo. El llamador es dueño de su tiempo de vida, su base address y su pipeline de handlers; la biblioteca es dueña de las URLs, las opciones de JSON y la forma del error.

## Cómo probarlo

Los tests reemplazan la red por un `StubHandler`, así que no necesitan servidor ni puerto abierto. Ejecuta `dotnet test SdkLibraries.sln` desde esta carpeta.

## Escribe primero el test

El cliente actual es el punto de partida. El test precede al cambio de ruta porque la URL forma parte del contrato de la API.

El test reemplaza la red por un handler que registra la petición:

```csharp
[TestMethod]
public async Task GetTasksReadsTheListFromTheApi()
{
    var id = Guid.NewGuid();
    var handler = new StubHandler(
        HttpStatusCode.OK,
        $"[{{\"id\":\"{id}\",\"title\":\"Write the chapter\",\"isDone\":false}}]");

    var client = CreateClient(handler);

    var tasks = await client.GetTasksAsync();

    Assert.AreEqual(1, tasks.Count);
    Assert.AreEqual(id, tasks[0].Id);
    Assert.AreEqual("Write the chapter", tasks[0].Title);
    Assert.AreEqual(HttpMethod.Get, handler.Request!.Method);
    Assert.AreEqual("https://api.test/tasks", handler.Request.RequestUri!.ToString());
}
```

`StubHandler` deriva de `HttpMessageHandler` y devuelve una respuesta fija:

```csharp
protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
{
    Request = request;

    if (request.Content is not null)
    {
        RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
    }

    return new HttpResponseMessage(_status)
    {
        Content = new StringContent(_body, Encoding.UTF8, "application/json")
    };
}
```

Sin servidor, sin puerto y sin red. El test igual revisa el método, la URL y la deserialización.

## Intenta ejecutar el test

Apunta el cliente a `task` en vez de `tasks` y el test lo atrapa:

```text
Assert.AreEqual failed. Expected:<https://api.test/tasks>. Actual:<https://api.test/task>.
```

La URL es parte del contrato, así que va en una aserción. Un test que solo revisa el objeto devuelto pasaría contra un stub que ignora la ruta.

## Escribe el código mínimo para que el test compile y mira la salida

El cliente puede deserializar la respuesta con esta línea, pero la URL está mal:

```csharp
using var response = await _http.GetAsync("task", cancellationToken);
```

## Escribe el código suficiente para que pase

Usa la ruta plural que expone la API:

```csharp
using var response = await _http.GetAsync("tasks", cancellationToken);
```

Vuelve a ejecutar la solución; pasan los tests de lectura, escritura y error.

## Refactor

### Empaquétalo

```sh
dotnet pack src/TaskTracker.Client/TaskTracker.Client.csproj -c Release -o /tmp/sdk-pack
```

```text
Successfully created package '/tmp/sdk-pack/TaskTracker.Client.1.0.0.nupkg'.
```

El paquete pesa 9 KB y contiene:

```text
lib/net6.0/TaskTracker.Client.dll
lib/net6.0/TaskTracker.Client.xml
README.md
TaskTracker.Client.nuspec
```

El `.nuspec` se genera desde el archivo de proyecto:

```xml
<id>TaskTracker.Client</id>
<version>1.0.0</version>
<authors>Learn .NET with Tests</authors>
<readme>README.md</readme>
<description>Typed HTTP client for the TaskTracker API.</description>
```

### Versiona la superficie pública

El id y la versión del paquete son parte del contrato. Quitar un método, cambiar un tipo de retorno o renombrar una propiedad de un record es un cambio incompatible; necesita una versión mayor. Agregar un parámetro opcional o un método nuevo es una versión menor.

El archivo de documentación XML y el README viajan dentro del paquete, así el consumidor ve la misma descripción que escribiste aquí. Un paquete que solo contiene un DLL obliga a cada consumidor a leer el código.

### Preocupaciones de producción

- Usa `IHttpClientFactory` o un registro de cliente tipado en la app que consume. Crear un `HttpClient` nuevo por llamada agota los sockets.
- Pon la autenticación en un `DelegatingHandler`, no en cada método. El refresh del token va ahí.
- Agrega timeout y cancellation token a cada llamada. La biblioteca no debería decidir que una petición puede colgarse para siempre.
- Genera Source Link y builds deterministas si el paquete se publica; el consumidor debería poder entrar al código fuente.
- Corre `dotnet pack` en CI y guarda el `.nupkg` como artefacto. El paquete es el entregable, no un efecto secundario de un build local.
- Agrega un chequeo de compatibilidad antes de un release, como `Microsoft.DotNet.ApiCompat`, para que un miembro público removido rompa el build.

## Repite con requisitos nuevos

Los siguientes requisitos son crear una tarea y mapear una respuesta de error. El segundo test devuelve `201 Created` y comprueba el POST:

```csharp
Assert.AreEqual(HttpMethod.Post, handler.Request!.Method);
StringAssert.Contains(handler.RequestBody, "Write the chapter");
Assert.AreEqual("application/json", handler.Request.Content!.Headers.ContentType!.MediaType);
```

El tercero devuelve `400 Bad Request` y espera la excepción tipada:

```csharp
var error = await Assert.ThrowsExceptionAsync<TaskClientException>(
    () => client.CreateTaskAsync(""));

Assert.AreEqual(HttpStatusCode.BadRequest, error.StatusCode);
StringAssert.Contains(error.Message, "title is required");
```

`TaskClientException` lleva el estado y el cuerpo. Un llamador puede mostrar el mensaje o mapear el estado sin parsear un string.

## Lo que queda diferido

El paquete no se publica en nuget.org ni en un feed privado, y no está firmado. Un laboratorio práctico agregaría un secreto `NUGET_API_KEY`, un step de `dotnet nuget push` condicionado a un tag y un smoke test que instale el paquete en un proyecto de consola nuevo.

## Para cerrar

Lo que cubrimos:

- Una biblioteca de clases con metadatos de paquete, README y documentación XML.
- Un cliente tipado que es dueño de las URLs, las opciones de JSON y el manejo de errores.
- Tres tests contra un stub handler: lectura, escritura y fallo.
- La aserción de URL que atrapa una ruta equivocada.
- `dotnet pack`, los archivos dentro del paquete y las reglas de versionado de su superficie pública.
- Tiempo de vida, handlers de auth, Source Link y empaquetado en CI como preocupaciones de producción.

Sigue: tests end-to-end con Playwright.
