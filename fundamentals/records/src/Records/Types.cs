namespace Records;

public struct Counter
{
    public int Value { get; set; }

    public void Increment()
    {
        Value++;
    }
}

public record Person(string Name, int Age)
{
    public bool IsAdult => Age >= 18;
}

