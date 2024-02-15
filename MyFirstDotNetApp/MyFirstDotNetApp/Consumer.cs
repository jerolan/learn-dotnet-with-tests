namespace MyFirstDotNetApp;

public class Consumer
{
    private readonly IGreetingService _greetingService;

    public Consumer(IGreetingService greetingService)
    {
        _greetingService = greetingService;
    }

    public string SayHello(string name)
    {
        return _greetingService.Greet(name);
    }
}