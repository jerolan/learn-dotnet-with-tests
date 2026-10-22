namespace Tasks;

public static class TaskCombinators
{
    public static async Task<int> SumAsync(params Task<int>[] tasks)
    {
        var values = await Task.WhenAll(tasks);

        var sum = 0;

        foreach (var value in values)
        {
            sum += value;
        }

        return sum;
    }

    public static async Task<int> FirstAsync(params Task<int>[] tasks)
    {
        var completed = await Task.WhenAny(tasks);

        return await completed;
    }
}

