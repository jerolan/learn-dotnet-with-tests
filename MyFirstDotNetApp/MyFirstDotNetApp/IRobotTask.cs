namespace MyFirstDotNetApp;

public interface IRobotTask
{
    string TaskName { get; }
    string PerformTask();
}