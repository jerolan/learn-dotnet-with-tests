# Tests de rendimiento con NBomber

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/performance-nbomber)

Un test de rendimiento responde "cuánta carga aguanta esto y dónde se hace lento". No es un test unitario y no pertenece al loop normal de CI. Este apéndice corre un escenario de NBomber contra una API mínima y explica cómo leer el resultado.

## El objetivo

`src/TargetApi` es una API mínima con un endpoint:

```csharp
app.MapGet("/ping", () => Results.Ok(new { status = "ok" }));
```

Es pequeño a propósito. El test mide el costo del cliente y del servidor, no una base de datos ni un servicio de terceros.

## El escenario

```csharp
var baseUrl = Environment.GetEnvironmentVariable("TARGET_URL") ?? "http://localhost:5099";
var httpClient = new HttpClient();

var scenario = Scenario.Create("ping", async context =>
{
    var request = Http.CreateRequest("GET", Targets.PingUrl(baseUrl));
    var response = await Http.Send(httpClient, request);

    return response;
})
.WithWarmUpDuration(TimeSpan.FromSeconds(1))
.WithLoadSimulations(
    Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(5)));

NBomberRunner.RegisterScenarios(scenario).Run();
```

Diez usuarios virtuales corren durante cinco segundos después de un calentamiento de un segundo. NBomber registra cada petición, el código de estado y la latencia.

## Ejecútalo

Abre una terminal para la API:

```sh
dotnet run --project src/TargetApi --urls http://localhost:5099
```

Abre otra para el test de carga:

```sh
dotnet run --project src/LoadTest
```

Define `TARGET_URL` para apuntar a otro host:

```sh
TARGET_URL=https://localhost:5001 dotnet run --project src/LoadTest
```

Una corrida del ejemplo contra localhost produjo una tabla como esta:

```text
request count: all = 68082, ok = 68082, RPS = 13616.4
latency: min = 0.06, mean = 0.68, max = 31.31, StdDev = 0.76
latency percentile: p50 = 0.46, p75 = 0.77, p95 = 1.79, p99 = 3.78
```

Los números dependen de la máquina. Lo que importa es la forma: throughput, distribución de latencia y cantidad de errores.

## Leer el resultado

- Las peticiones por segundo son el throughput bajo esta carga. No son un límite de capacidad; otra carga puede cambiarlo.
- La latencia media esconde valores atípicos. Lee p95 y p99: describen lo que ven los usuarios más lentos.
- Un conteo de errores distinto de cero invalida los números de latencia de las peticiones fallidas. Mira primero la tabla de códigos de estado.
- El calentamiento deja que el JIT y los pools de conexiones se estabilicen. Sin él, las primeras peticiones sesgan el resultado.

No compares una corrida en una laptop con una corrida en producción. Compara el mismo entorno antes y después de un cambio, con el mismo escenario y la misma carga.

## Refactor

Un test de rendimiento útil sigue la misma disciplina que el resto del libro:

- Mide un camino realista. Un endpoint `/ping` mide el framework; una lista de tareas con base de datos mide la característica.
- Mantén la carga fija al comparar cambios. Cambiar el escenario y el código en la misma corrida no dice nada.
- Corre en un entorno aislado. Otro proceso en la misma máquina compite por CPU y conexiones.
- Mantén el escenario fuera del job normal de tests. Tarda segundos y produce números dependientes de la máquina; córrelo a demanda o en un job dedicado.
- Registra una línea base y compara percentiles, no un promedio.
- Perfila antes de optimizar. Un serializador JSON más rápido no arregla un índice faltante.

K6 es una buena alternativa cuando el test de carga vive fuera de .NET o el equipo ya usa Grafana k6. La forma del escenario es la misma: usuarios virtuales fijos, una duración y un resultado con percentiles.

## Nota de deferral

El test de carga es una app de consola, no un proyecto de MSTest, así que CI lo compila pero no ejecuta la carga. Un gate de rendimiento en CI necesita un entorno dedicado y una línea base estable; eso queda fuera del alcance de este libro. El apéndice entrega el escenario runnable y el método de lectura.

## Para cerrar

Lo que cubrimos:

- Un escenario de NBomber con usuarios virtuales, calentamiento y duración fija.
- La API objetivo y el flujo de dos terminales.
- Throughput, percentiles de latencia y conteo de errores.
- Entornos aislados, líneas base y perfilar antes de optimizar.
- Por qué el test de carga se queda fuera del job normal.

Siguiente: contenedores.

