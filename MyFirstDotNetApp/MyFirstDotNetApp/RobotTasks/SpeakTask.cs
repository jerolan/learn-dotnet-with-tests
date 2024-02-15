namespace MyFirstDotNetApp.RobotTasks;

public class SpeakTask : IRobotTask
{
    public string TaskName => "Speak";

    public string PerformTask()
    {
        return "Speaking";
    }
}