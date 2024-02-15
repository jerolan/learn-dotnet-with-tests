using System.Threading.Tasks;

public class DataService
{
    public async Task<string> FetchDataAsync()
    {
        // Simulate a delay to mimic an asynchronous operation
        await Task.Delay(1000); // Wait for 1 second
        return "Expected Data";
    }
}
