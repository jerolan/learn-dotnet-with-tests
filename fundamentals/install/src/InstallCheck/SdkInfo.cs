namespace InstallCheck;

public static class SdkInfo
{
    public static string GetRuntimeVersion()
    {
        return System.Environment.Version.ToString();
    }
}
