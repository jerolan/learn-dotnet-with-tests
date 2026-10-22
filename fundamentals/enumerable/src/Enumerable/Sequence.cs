namespace Enumerable;

public static class Sequence
{
    public static IEnumerable<int> Naturals()
    {
        var current = 0;

        while (true)
        {
            yield return current;
            current++;
        }
    }

    public static IEnumerable<int> Evens(int max)
    {
        for (var i = 0; i <= max; i += 2)
        {
            yield return i;
        }
    }

    public static IEnumerable<int> Tracked(List<int> log)
    {
        log.Add(1);
        yield return 1;

        log.Add(2);
        yield return 2;
    }
}

