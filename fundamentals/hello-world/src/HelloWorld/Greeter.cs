namespace HelloWorld;

public class Greeter
{
    private const string EnglishHelloPrefix = "Hello, ";

    public string SayHello(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = "World";
        }

        return EnglishHelloPrefix + name;
    }
}
