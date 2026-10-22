namespace LoadTest;

public static class Targets
{
    public static string PingUrl(string baseUrl)
    {
        return new Uri(new Uri(baseUrl), "/ping").ToString();
    }
}

