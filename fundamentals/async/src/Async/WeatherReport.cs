namespace Async;

public interface ITemperatureSource
{
    Task<int> GetAsync(string city, CancellationToken cancellationToken);
}

public sealed class WeatherReport
{
    private readonly ITemperatureSource _source;

    public WeatherReport(ITemperatureSource source)
    {
        _source = source;
    }

    public async Task<string> DescribeAsync(string city, CancellationToken cancellationToken = default)
    {
        var temperature = await _source.GetAsync(city, cancellationToken);

        return $"{city}: {temperature}C";
    }
}

