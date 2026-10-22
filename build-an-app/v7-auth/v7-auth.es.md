# v7: Autenticación con JWT

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v7-auth)

Todos los endpoints anteriores eran anónimos. La versión 7 agrega un endpoint de token, firma un JWT con una clave simétrica y protege las rutas de tareas. Una petición sin token válido recibe un 401 antes de llegar al handler.

La solución terminada está en `build-an-app/v7-auth`.

## Escribe primero el test

```csharp
[TestMethod]
public async Task TasksWithoutToken_ReturnUnauthorized()
{
    using var api = new Api(authenticate: false);

    var response = await api.Client.GetAsync("/tasks");

    Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

El driver puede saltear la autenticación para este test. Falla con `Expected:<Unauthorized>. Actual:<OK>` contra la API anónima de v6.

## Agrega el paquete y las opciones

```sh
dotnet add src/TaskTracker.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 6.0.36
```

```csharp
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TaskTracker";

    public string Audience { get; set; } = "TaskTracker";

    public string Key { get; set; } = "development-key-for-tasktracker-0123456789";

    public int ExpiryMinutes { get; set; } = 60;

    public string DemoUser { get; set; } = "admin";

    public string DemoPassword { get; set; } = "password";
}
```

La sección `Jwt` de `appsettings.json` provee los valores. Las credenciales de demostración existen para que el capítulo corra sin un proveedor de identidad; no son un diseño de producción.

## Configura la autenticación

```csharp
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
```

y agrega el middleware antes de los endpoints:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

## Emite un token

```csharp
app.MapPost("/auth/token", (LoginRequest request, IOptions<JwtOptions> options) =>
{
    var settings = options.Value;

    if (request.Username != settings.DemoUser || request.Password != settings.DemoPassword)
    {
        return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, request.Username),
        new Claim(ClaimTypes.Name, request.Username)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        settings.Issuer,
        settings.Audience,
        claims,
        expires: DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes),
        signingCredentials: credentials);

    return Results.Ok(new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token)));
});
```

## Protege las rutas

```csharp
app.MapGet("/tasks", (ITaskReader reader) => reader.All()).RequireAuthorization();

app.MapGet("/tasks/{id:int}", (int id, ITaskReader reader) =>
{
    // ...
}).RequireAuthorization();
```

Agrega `.RequireAuthorization()` también a POST y PATCH. El endpoint del token queda anónimo.

## Cómo funciona

Un JWT tiene tres partes: un header, un payload de claims y una firma. El servidor firma las dos primeras con la clave simétrica. En cada petición, el middleware de JWT lee la cabecera `Authorization: Bearer <token>`, valida la firma, el emisor, la audiencia y el tiempo de vida, y construye un `ClaimsPrincipal` para la petición.

`ClockSkew = TimeSpan.Zero` hace exacta la comprobación de vida, útil en tests y para tokens cortos. En producción, un margen pequeño (el valor por defecto es cinco minutos) absorbe diferencias de reloj entre servidores.

Autenticación responde "quién eres"; autorización responde "puedes hacer esto". `RequireAuthorization()` rechaza una petición no autenticada con 401. Una política o un rol rechazaría a un usuario autenticado pero no autorizado con 403; esta versión solo necesita la primera barrera.

## Repite con requisitos nuevos

### Credenciales inválidas

```csharp
[TestMethod]
public async Task TokenEndpoint_WithInvalidCredentials_ReturnsUnauthorized()
{
    using var api = new Api(authenticate: false);

    var response = await api.Client.PostAsJsonAsync(
        "/auth/token",
        new LoginRequest("admin", "wrong"));

    Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

Pasa. El endpoint del token no revela si falló el usuario o la contraseña; los dos producen el mismo 401.

### Un token autoriza la API

```csharp
[TestMethod]
public async Task TokenEndpoint_ReturnsATokenThatAuthorizesTheApi()
{
    using var api = new Api(authenticate: false);

    var token = await api.GetTokenAsync();
    Assert.IsFalse(string.IsNullOrWhiteSpace(token));

    api.Client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var response = await api.Client.GetAsync("/tasks");

    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}
```

Verde. El test obtiene un token del endpoint, lo adjunta y llega a la ruta protegida.

El driver de tests se autentica por defecto, así que el resto de la suite no cambió:

```csharp
if (authenticate)
{
    var token = GetTokenAsync().GetAwaiter().GetResult();
    Client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
}
```

Esa llamada síncrona es aceptable en la preparación de un test; el código de producción debe ser async de punta a punta.

## Refactor

Las credenciales de demostración y la clave simétrica son andamiaje didáctico. En un servicio real:

- Lee la clave de firma de un almacén de secretos, no de un archivo versionado.
- Hashea las contraseñas y usa un proveedor de identidad o un almacén de usuarios; nunca compares contraseñas en texto plano.
- Usa HTTPS. Un token bearer es una credencial; mandarlo por HTTP plano lo expone.
- Mantén los access tokens de vida corta y soporta rotación de la clave de firma.
- No loguees tokens ni cabeceras `Authorization` completas.

Los claims son mínimos a propósito. Un `sub` y un nombre alcanzan para autenticar; roles o scopes pertenecen a una política cuando un endpoint necesita autorización más fina.

## Para cerrar

Lo que cubrimos:

- Un objeto de opciones JWT y una clave simétrica de firma.
- `AddAuthentication`/`AddJwtBearer` con validación de emisor, audiencia y vida.
- Un endpoint de token con credenciales de demostración.
- `RequireAuthorization()` en las rutas de tareas y un 401 sin token.
- Un driver de tests que se autentica por defecto y un cliente crudo para el caso anónimo.
- Los límites de seguridad del setup didáctico.

Siguiente: [v8: tiempos de vida de servicios y un decorador](../v8-di/v8-di.es.md).

