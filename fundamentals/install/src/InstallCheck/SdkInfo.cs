namespace InstallCheck;

public static class SdkInfo
{
    public static string GetDotnetVersion()
    {
        return System.Environment.Version.ToString();
    }
}
