# v12: OpenAPI

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v12-openapi)

La API ya es lo bastante grande como para que quien llama necesite un contrato: qué rutas existen, qué cuerpos reciben, qué respuestas devuelven y cómo se autentican. La versión 12 genera un documento OpenAPI a partir de los endpoints y prueba que el documento coincide con la superficie.

La solución terminada está en `build-an-app/v12-openapi`.

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

    var security = root
        .GetProperty("components")
        .GetProperty("securitySchemes")
        .GetProperty("Bearer");

    Assert.AreEqual("http", security.GetProperty("type").GetString());
    Assert.AreEqual("bearer", security.GetProperty("scheme").GetString());
}
```

Contra v11 la ruta devuelve 404. El test lee el documento como JSON en vez de confiar en la UI.

## Agrega Swashbuckle

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
});
```

y sírvelo:

```csharp
app.UseSwagger();
app.UseSwaggerUI();
```

Verde. El documento está en `/swagger/v1/swagger.json` y la UI en `/swagger`.

## Cómo funciona

Swashbuckle lee la metadata que ASP.NET Core construyó a partir de los handlers: la ruta, el método HTTP, el tipo del cuerpo y los tipos de respuesta. Escribe un documento OpenAPI con `paths`, `components/schemas` y `components/securitySchemes`.

La definición y el requisito de seguridad son los que hacen que la UI muestre un botón Authorize y permiten pegar el JWT. El documento sigue listando los endpoints; el esquema de seguridad le dice al cliente cómo llamarlos.

El test fija el contrato. Un renombre de ruta, un endpoint eliminado o un tipo de request cambiado rompe el test antes de que lo descubra un consumidor. Es un test de contrato, no un test del formato de Swashbuckle.

## Refactor

Un documento generado solo sirve si se mantiene exacto:

- Agrega documentación XML y ejemplos cuando un campo necesite explicación humana. Activa `GenerateDocumentationFile` e incluye el archivo en `AddSwaggerGen`.
- Versiona el documento cuando la API cambie de forma incompatible. `SwaggerDoc("v2", ...)` y un plan de rutas versionadas son más baratos que sorprender a los clientes.
- Esconde endpoints internos. Una ruta de diagnóstico con ids de proceso sirve en desarrollo y no debería estar en un documento público; usa metadata o un documento separado.
- Prueba el documento en CI, como hace este capítulo. La UI es una comodidad; el JSON es el contrato.
- En .NET 9 y posteriores, el generador de OpenAPI integrado reemplaza la generación de documentos de Swashbuckle; el libro sigue con Swashbuckle porque el proyecto apunta a .NET 6.

El documento también expone el flujo de auth: un cliente llama a `/auth/token` sin token y después manda `Authorization: Bearer ...` a las rutas de tareas. El esquema de seguridad documenta ese flujo, no reemplaza al middleware.

## Para cerrar

Lo que cubrimos:

- `AddEndpointsApiExplorer` y `AddSwaggerGen` para APIs mínimas.
- Una API documentada con rutas, esquemas y un esquema Bearer.
- `UseSwagger` y la UI de Swagger.
- Un test de contrato que lee el JSON generado.
- Mantener el documento exacto con ejemplos, versionado y rutas internas ocultas.

Siguiente: [v13: resiliencia con Polly](../v13-resilience/v13-resilience.es.md).

