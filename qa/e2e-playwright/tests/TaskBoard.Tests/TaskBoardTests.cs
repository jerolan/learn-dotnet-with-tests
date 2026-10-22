using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskBoard.Tests;

[TestClass]
public sealed class TaskBoardTests
{
    private static Process _app = null!;
    private static string _baseUrl = null!;
    private static IPlaywright _playwright = null!;
    private static IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    [ClassInitialize]
    public static async Task StartEverything(TestContext context)
    {
        if (Environment.GetEnvironmentVariable("RUN_E2E") != "1")
        {
            return;
        }

        var root = FindRoot();
        var port = GetFreePort();
        _baseUrl = $"http://127.0.0.1:{port}";

        var dll = Path.Combine(
            root,
            "src",
            "TaskBoard",
            "bin",
            "Debug",
            "net6.0",
            "TaskBoard.dll");

        _app = Process.Start(new ProcessStartInfo("dotnet", $"\"{dll}\" --urls {_baseUrl}")
        {
            WorkingDirectory = Path.Combine(root, "src", "TaskBoard"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Could not start TaskBoard.");

        await WaitForHealthAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    [ClassCleanup]
    public static async Task StopEverything()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_app is not null && !_app.HasExited)
        {
            _app.Kill(entireProcessTree: true);
            _app.WaitForExit(5000);
        }
    }

    [TestInitialize]
    public async Task OpenPage()
    {
        if (Environment.GetEnvironmentVariable("RUN_E2E") != "1")
        {
            Assert.Inconclusive("Set RUN_E2E=1 and install Chromium to run the browser test.");
        }

        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        await _page.GotoAsync(_baseUrl);
    }

    [TestCleanup]
    public async Task ClosePage()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();
        }
    }

    [TestMethod]
    public async Task AddingATaskShowsItInTheListAndAfterAReload()
    {
        await _page.FillAsync("#title", "Write the chapter");
        await _page.ClickAsync("button[type=submit]");

        await _page.WaitForSelectorAsync("li:has-text('Write the chapter')");

        await _page.ReloadAsync();

        Assert.AreEqual(
            "Write the chapter",
            await _page.Locator("#tasks li").First.TextContentAsync());
    }

    private static async Task WaitForHealthAsync()
    {
        using var client = new HttpClient();

        for (var attempt = 0; attempt < 100; attempt++)
        {
            try
            {
                var response = await client.GetAsync($"{_baseUrl}/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(100);
        }

        throw new InvalidOperationException("TaskBoard did not become healthy.");
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "E2E.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not find the appendix root.");
    }
}
