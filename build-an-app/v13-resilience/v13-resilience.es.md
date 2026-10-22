# v13: Resiliencia con Polly

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v13-resilience)

Una dependencia externa falla a veces. Un reintento puede convertir un 503 transitorio en un éxito; también puede multiplicar el daño cuando el fallo es permanente. La versión 13 envuelve el notificador en una política de reintentos de Polly y prueba el presupuesto con un handler que falla a propósito.

La solución terminada está en `build-an-app/v13-resilience`.

## Escribe primero el test

```csharp
[TestMethod]
public async Task Notifications_RetryTransientFailures()
{
    using var api = new Api(notifierFailures: 2);

    var response = await api.Client.PostAsJsonAsync(
        "/notifications",
        new NotificationRequest("hello"));

    Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    Assert.AreEqual(3, api.NotifierCalls);
}
```

El host de test reemplaza el notificador por uno que usa un handler falso. El handler devuelve `503` en las dos primeras llamadas y `200` después; la aserción espera que el endpoint salga bien tras tres llamadas.

## Agrega el notificador

```sh
dotnet add src/TaskTracker.Api package Polly --version 7.2.4
```

```csharp
public interface INotifier
{
    Task SendAsync(string message);
}

public sealed class HttpNotifier : INotifier
{
    private readonly HttpClient _client;

    public HttpNotifier(HttpClient client)
    {
        _client = client;
    }

    public async Task SendAsync(string message)
    {
        using var response = await _client.PostAsJsonAsync("/notify", new { message });
        response.EnsureSuccessStatusCode();
    }
}
```

`EnsureSuccessStatusCode` lanza `HttpRequestException` ante un 5xx, que es la señal que maneja la política.

## Envuélvelo en una política

```csharp
public sealed class ResilientNotifier : INotifier
{
    private readonly INotifier _inner;
    private readonly IAsyncPolicy _policy;

    public ResilientNotifier(INotifier inner, IAsyncPolicy policy)
    {
        _inner = inner;
        _policy = policy;
    }

    public Task SendAsync(string message)
    {
        return _policy.ExecuteAsync(() => _inner.SendAsync(message));
    }
}
```

Registra el cliente tipado y el decorador:

```csharp
builder.Services.AddHttpClient<HttpNotifier>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Notifier:BaseUrl"] ?? "http://notifier"));

builder.Services.AddScoped<INotifier>(services =>
    new ResilientNotifier(
        services.GetRequiredService<HttpNotifier>(),
        Policy.Handle<HttpRequestException>().RetryAsync(2)));
```

Agrega el endpoint:

```csharp
app.MapPost("/notifications", async (NotificationRequest request, INotifier notifier) =>
{
    await notifier.SendAsync(request.Message);

    return Results.Accepted();
}).RequireAuthorization();
```

Verde. La política reintenta dos veces, así que la tercera llamada es la que sale bien.

## Cómo funciona

`Policy.Handle<HttpRequestException>()` dice qué fallos son reintentables. `RetryAsync(2)` corre la acción hasta dos veces más después del primer fallo. El handler cuenta cada intento, incluido el primero.

La política vive en el decorador, no en `HttpNotifier`. El cliente HTTP queda como un adaptador simple; la decisión de resiliencia es una preocupación aparte y puede cambiar sin tocar el código de la petición.

Un reintento solo es seguro cuando la operación es idempotente. Mandar dos veces una notificación puede molestar a una persona; cobrar dos veces una tarjeta es inaceptable. Para llamadas no idempotentes, usa una clave de idempotencia y un contrato de deduplicación del lado del servidor antes de agregar reintentos.

## Repite con requisitos nuevos

### Rendirse después del presupuesto

```csharp
[TestMethod]
public async Task Notifications_GiveUpAfterTheRetryBudget()
{
    using var api = new Api(notifierFailures: 10);

    var response = await api.Client.PostAsJsonAsync(
        "/notifications",
        new NotificationRequest("hello"));

    Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    Assert.AreEqual(3, api.NotifierCalls);
}
```

Verde. El tercer fallo escapa de la política y el endpoint devuelve 500. El test fija la cantidad de intentos: una llamada inicial más dos reintentos, ni uno más.

### Sin reintento cuando hay éxito

```csharp
[TestMethod]
public async Task Notifications_DoNotRetrySuccess()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync(
        "/notifications",
        new NotificationRequest("hello"));

    Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    Assert.AreEqual(1, api.NotifierCalls);
}
```

Verde. La política solo observa fallos; una llamada exitosa se devuelve de inmediato.

## Refactor

El `RetryAsync(2)` fijo es una política didáctica. Producción necesita más criterio:

- Usa backoff exponencial con jitter para que una dependencia caída no reciba golpes de todos los clientes a la vez.
- Agrega un timeout por intento. Reintentar una llamada que se cuelga para siempre no es resiliencia.
- Agrega un circuit breaker tras fallos repetidos para que quien llama falle rápido mientras la dependencia se recupera.
- Reintenta solo fallos transitorios: timeouts, 5xx y errores de conexión. Un 400 o un error de validación no va a mejorar en el segundo intento.
- Loguea el número de intento y el id de correlación. Un reintento que nadie ve es difícil de diagnosticar.
- Usa el `HttpClient` tipado de la factory; no crees un `HttpClient` nuevo por llamada.

Polly 8 expone las mismas ideas con `ResiliencePipeline`. La forma es la misma: manejar fallos seleccionados, reintentar un número acotado de veces, sumar timeout y breaker, y mantener la política fuera del adaptador.

## Para cerrar

Lo que cubrimos:

- Un notificador con `HttpClient` tipado detrás de una interfaz.
- Un decorador de reintentos de Polly con presupuesto acotado.
- Un handler falso que controla fallos y cuenta intentos.
- Tests de recuperación, agotamiento y camino exitoso.
- Idempotencia, backoff, timeout, circuit breaker y observabilidad como siguientes pasos.

Siguiente: [v14: CQRS con MediatR](../v14-cqrs/v14-cqrs.es.md).

