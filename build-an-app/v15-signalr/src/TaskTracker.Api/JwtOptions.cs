namespace TaskTracker.Api;

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
