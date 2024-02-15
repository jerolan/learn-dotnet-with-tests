namespace MyFirstDotNetApp;

public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }

    public string IntroduceYourself()
    {
        return $"Hello, my name is {Name} and I am {Age} years old.";
    }
}