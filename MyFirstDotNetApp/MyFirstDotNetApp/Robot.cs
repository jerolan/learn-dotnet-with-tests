namespace MyFirstDotNetApp;

public class Robot
{
    private readonly Dictionary<string, IRobotTask> _tasks;

    public Robot(IEnumerable<IRobotTask> tasks)
    {
        _tasks = tasks.ToDictionary(task => task.TaskName);
    }

    public string PerformTask(string taskName)
    {
        if (_tasks.TryGetValue(taskName, out var task)) return task.PerformTask();
        return "Task not found";
    }
}