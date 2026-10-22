namespace Exceptions;

public sealed class InvalidAgeException : Exception
{
    public InvalidAgeException(string message)
        : base(message)
    {
    }
}

public static class Age
{
    private const int MaxAge = 120;

    public static int Parse(string text)
    {
        if (!int.TryParse(text, out var age))
        {
            throw new InvalidAgeException($"'{text}' is not a number");
        }

        if (age < 0 || age > MaxAge)
        {
            throw new InvalidAgeException($"'{text}' is outside the valid age range");
        }

        return age;
    }
}

