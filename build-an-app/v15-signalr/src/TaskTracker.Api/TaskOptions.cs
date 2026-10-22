namespace TaskTracker.Api;

public sealed class TaskOptions
{
    public const string SectionName = "Tasks";

    public int MaxTitleLength { get; set; } = 120;
}
