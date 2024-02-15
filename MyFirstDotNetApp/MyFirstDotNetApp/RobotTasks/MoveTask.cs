namespace MyFirstDotNetApp.RobotTasks;

public class MoveTask : IRobotTask
{
    public string TaskName => "Move";

    public string PerformTask()
    {
        return "Moving";
    }
}