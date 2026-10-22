namespace Composition;

public interface ITextTransform
{
    string Apply(string input);
}

public sealed class Trim : ITextTransform
{
    public string Apply(string input)
    {
        return input.Trim();
    }
}

public sealed class UpperCase : ITextTransform
{
    public string Apply(string input)
    {
        return input.ToUpperInvariant();
    }
}

public sealed class Prefix : ITextTransform
{
    private readonly string _prefix;

    public Prefix(string prefix)
    {
        _prefix = prefix;
    }

    public string Apply(string input)
    {
        return _prefix + input;
    }
}

public sealed class TextPipeline
{
    private readonly IEnumerable<ITextTransform> _steps;

    public TextPipeline(IEnumerable<ITextTransform> steps)
    {
        _steps = steps;
    }

    public string Apply(string input)
    {
        var result = input;

        foreach (var step in _steps)
        {
            result = step.Apply(result);
        }

        return result;
    }
}

