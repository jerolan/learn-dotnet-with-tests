namespace TaskTracker.Api;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int Seconds { get; set; } = 30;
}
