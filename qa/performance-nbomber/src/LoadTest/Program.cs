using LoadTest;
using NBomber.CSharp;
using NBomber.Http.CSharp;

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

