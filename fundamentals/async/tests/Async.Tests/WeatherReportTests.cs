using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Async.Tests;

[TestClass]
public sealed class WeatherReportTests
{
    [TestMethod]
    public async Task DescribeAsync_ReturnsTheFormattedReport()
    {
        var source = new FakeSource((_, _) => Task.FromResult(20));
        var report = new WeatherReport(source);

        Assert.AreEqual("Madrid: 20C", await report.DescribeAsync("Madrid"));
    }

    [TestMethod]
    public async Task DescribeAsync_PropagatesTheSourceException()
    {
        var source = new FakeSource((_, _) =>
            Task.FromException<int>(new InvalidOperationException("offline")));
        var report = new WeatherReport(source);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => report.DescribeAsync("Madrid"));
    }

    [TestMethod]
    public async Task DescribeAsync_CanceledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var source = new FakeSource((_, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(20);
        });
        var report = new WeatherReport(source);

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => report.DescribeAsync("Madrid", cts.Token));
    }

    private sealed class FakeSource : ITemperatureSource
    {
        private readonly Func<string, CancellationToken, Task<int>> _get;

        public FakeSource(Func<string, CancellationToken, Task<int>> get)
        {
            _get = get;
        }

        public Task<int> GetAsync(string city, CancellationToken cancellationToken)
        {
            return _get(city, cancellationToken);
        }
    }
}

