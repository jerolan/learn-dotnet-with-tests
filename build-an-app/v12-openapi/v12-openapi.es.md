# v12: OpenAPI

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v12-openapi)

La API ya es lo bastante grande como para que quien llama necesite un contrato: qué rutas existen, qué cuerpos reciben, qué respuestas devuelven y cómo se autentican. La versión 12 genera un documento OpenAPI a partir de los endpoints y prueba que el documento coincide con la superficie.

La solución terminada está en `build-an-app/v12-openapi`.

## Cómo funciona

El explorador de API de ASP.NET Core describe las rutas de la API mínima. Swashbuckle lee esa metadata y genera un documento OpenAPI con rutas y esquemas. `UseSwagger` sirve el JSON; `UseSwaggerUI` sirve una página interactiva para explorarlo.

La autenticación sigue a cargo del middleware de la aplicación. El esquema de seguridad de OpenAPI documenta cómo envía el token el cliente; no protege una ruta.

## Cómo probarlo

Solicita `/swagger/v1/swagger.json` por HTTP y analiza el JSON. Comprueba partes estables del contrato: nombres de rutas, nombres de esquemas y, en un test posterior, el esquema de seguridad Bearer. Así verificas el documento que reciben sus consumidores sin probar el formato de Swashbuckle ni depender de la UI.

## Escribe primero el test

```csharp
[TestMethod]
public async Task SwaggerDocument_ListsTheApiSurface()
{
    using var api = new Api();

    var json = await api.Client.GetStringAsync("/swagger/v1/swagger.json");

    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;
    var paths = root.GetProperty("paths");

    Assert.IsTrue(paths.TryGetProperty("/tasks", out _));
    Assert.IsTrue(paths.TryGetProperty("/tasks/{id}", out _));
    Assert.IsTrue(paths.TryGetProperty("/tasks/{id}/done", out _));
    Assert.IsTrue(paths.TryGetProperty("/auth/token", out _));
    Assert.IsTrue(paths.TryGetProperty("/diagnostics", out _));

    var schemas = root.GetProperty("components").GetProperty("schemas");

    Assert.IsTrue(schemas.TryGetProperty("TaskItem", out _));
    Assert.IsTrue(schemas.TryGetProperty("CreateTaskRequest", out _));
}
```

## Intenta ejecutar el test

Contra v11, `GET /swagger/v1/swagger.json` devuelve 404 porque la aplicación no expone un documento OpenAPI. `GetStringAsync` falla ante esa respuesta no exitosa.

## Escribe el código mínimo para que el test compile y mira la salida

Instala Swashbuckle y registra el explorador de API y un documento con nombre:

```sh
dotnet add src/TaskTracker.Api package Swashbuckle.AspNetCore --version 6.5.0
```

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskTracker API",
        Version = "v1",
        Description = "Task tracker used in the Learn .NET with Tests book."
    });
});
```

El test todavía recibe 404: el documento ya está configurado, pero falta agregar el middleware al pipeline de peticiones.

## Escribe el código suficiente para que pase

Expón el documento y la UI:

```csharp
app.UseSwagger();
app.UseSwaggerUI();
```

El test de la superficie de la API ya pasa: el JSON contiene las rutas y los esquemas esperados.

## Refactor

Un documento generado solo sirve mientras se mantenga exacto:

- Agrega documentación XML y ejemplos cuando un campo necesite una explicación humana. Activa `GenerateDocumentationFile` e incluye el archivo en `AddSwaggerGen`.
- Versiona el documento cuando la API cambie de forma incompatible. `SwaggerDoc("v2", ...)` y un plan de rutas versionadas son más baratos que sorprender a los clientes.
- Oculta endpoints internos. Una ruta de diagnóstico con ids de proceso sirve en desarrollo y no debería estar en un documento público; usa metadata de endpoint o un documento separado.
- Prueba el documento en CI, como hace este capítulo. La UI es una comodidad; el JSON es el contrato.
- ASP.NET Core incluye generación OpenAPI integrada desde .NET 9, pero no incluye una UI interactiva de forma predeterminada. Este libro usa Swashbuckle para su solución .NET 6 ([documentación de Microsoft](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)).

## Repite con requisitos nuevos

### Describe la autenticación Bearer

Escribe un test para las instrucciones de autenticación del documento:

```csharp
[TestMethod]
public async Task SwaggerDocument_DescribesBearerAuthentication()
{
    using var api = new Api();

    var json = await api.Client.GetStringAsync("/swagger/v1/swagger.json");

    using var document = JsonDocument.Parse(json);
    var security = document.RootElement
        .GetProperty("components")
        .GetProperty("securitySchemes")
        .GetProperty("Bearer");

    Assert.AreEqual("http", security.GetProperty("type").GetString());
    Assert.AreEqual("bearer", security.GetProperty("scheme").GetString());
}
```

Falla porque el documento generado no tiene un esquema de seguridad Bearer. Agrega la definición y el requisito dentro del bloque de opciones de `AddSwaggerGen` que ya existe:

```csharp
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Paste the accessToken returned by POST /auth/token."
});

options.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
});
```

El test pasa. Un cliente llama a `/auth/token` sin token y después envía `Authorization: Bearer ...` a las rutas de tareas. El esquema documenta ese flujo; el middleware de autenticación sigue haciéndolo cumplir.

## Para cerrar

Lo que cubrimos:

- `AddEndpointsApiExplorer` y `AddSwaggerGen` para APIs mínimas.
- Una API documentada con rutas, esquemas y un esquema Bearer.
- `UseSwagger` y la UI de Swagger.
- Un test de contrato que lee el JSON generado.
- Mantener el documento exacto con ejemplos, versionado y rutas internas ocultas.

Siguiente: [v13: resiliencia con Polly](../v13-resilience/v13-resilience.es.md).
