using System.Text;

namespace ControlFlow;

public static class ControlFlowExamples
{
    public static string Repeat(string text, int times)
    {
        var result = new StringBuilder();

        for (var i = 0; i < times; i++)
        {
            result.Append(text);
        }

        return result.ToString();
    }

    public static int SumTo(int limit)
    {
        var sum = 0;
        var current = 1;

        while (current <= limit)
        {
            sum += current;
            current++;
        }

        return sum;
    }

    public static string FizzBuzz(int number)
    {
        return (number % 3, number % 5) switch
        {
            (0, 0) => "FizzBuzz",
            (0, _) => "Fizz",
            (_, 0) => "Buzz",
            _ => number.ToString()
        };
    }
}

