namespace MyFirstDotNetApp;

public class GreetingService : IGreetingService
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }
}