# Performance Testing with NBomber

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/performance-nbomber)

A performance test answers "how much load can this handle, and where does it slow down?" It is not a unit test and it does not belong in the normal CI loop. This appendix runs an NBomber scenario against a minimal API and explains how to read the result.

## The target

`src/TargetApi` is a minimal API with one endpoint:

```csharp
app.MapGet("/ping", () => Results.Ok(new { status = "ok" }));
```

It is small on purpose. The load test measures the client and the server overhead, not a database or a third-party service.

## The scenario

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

Ten virtual users run for five seconds after a one-second warm-up. NBomber records every request, the status code and the latency.

## Run it

Open one terminal for the API:

```sh
dotnet run --project src/TargetApi --urls http://localhost:5099
```

Open another for the load test:

```sh
dotnet run --project src/LoadTest
```

Set `TARGET_URL` to point at another host:

```sh
TARGET_URL=https://localhost:5001 dotnet run --project src/LoadTest
```

A run of the sample against localhost produced a table like this:

```text
request count: all = 68082, ok = 68082, RPS = 13616.4
latency: min = 0.06, mean = 0.68, max = 31.31, StdDev = 0.76
latency percentile: p50 = 0.46, p75 = 0.77, p95 = 1.79, p99 = 3.78
```

The numbers depend on the machine. The shape is what matters: throughput, the latency distribution and the error count.

## Reading the result

- Requests per second is throughput under this load. It is not a capacity limit; a higher or lower load may change it.
- The mean latency hides outliers. Read p95 and p99: they describe the requests your slowest users see.
- A nonzero error count invalidates the latency numbers for the failed requests. Check the status-code table first.
- The warm-up lets the JIT and connection pools settle. Without it, the first requests skew the result.

Do not compare a run on a laptop with a run in production. Compare the same environment before and after a change, with the same scenario and the same load.

## Refactor

A useful performance test follows the same discipline as the rest of the book:

- Measure a realistic path. A `/ping` endpoint measures the framework; a task list with a database measures the feature.
- Keep the load fixed while comparing changes. Changing the scenario and the code in the same run tells you nothing.
- Run in an isolated environment. Another process on the same machine competes for CPU and connections.
- Keep the scenario out of the normal test job. It takes seconds and produces machine-dependent numbers; run it on demand or in a dedicated job.
- Record a baseline and compare percentiles, not one average.
- Profile before optimizing. A faster JSON serializer will not fix a missing index.

K6 is a good alternative when the load test lives outside .NET or the team already uses Grafana k6. The scenario shape is the same: a fixed number of virtual users, a duration, and a result with percentiles.

## Deferral note

The load test is a console app, not an MSTest project, so CI builds it but does not run the load. A performance gate in CI needs a dedicated environment and a stable baseline; that is outside this book's scope. The appendix gives you the runnable scenario and the reading method.

## Wrapping up

What we have covered:

- An NBomber scenario with virtual users, warm-up and a fixed duration.
- The target API and the two-terminal workflow.
- Throughput, latency percentiles and error counts.
- Isolated environments, baselines and profiling before optimizing.
- Why the load test stays out of the normal test job.

Next: containers.

