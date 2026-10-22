namespace Collections;

public sealed class WordCounter
{
    private readonly Dictionary<string, int> _counts;

    public WordCounter(string text)
    {
        _counts = CountWords(text);
    }

    public IReadOnlyDictionary<string, int> Counts => _counts;

    public IReadOnlyList<string> MostCommon(int count)
    {
        var entries = new List<KeyValuePair<string, int>>(_counts);

        entries.Sort((left, right) =>
        {
            var byCount = right.Value.CompareTo(left.Value);

            return byCount != 0
                ? byCount
                : string.CompareOrdinal(left.Key, right.Key);
        });

        var result = new List<string>();

        for (var i = 0; i < count && i < entries.Count; i++)
        {
            result.Add(entries[i].Key);
        }

        return result;
    }

    private static Dictionary<string, int> CountWords(string text)
    {
        var counts = new Dictionary<string, int>();
        var separators = new[] { ' ', '\t', '\n', '\r' };

        foreach (var word in text.Split(separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var key = word.ToLowerInvariant();

            counts.TryGetValue(key, out var current);
            counts[key] = current + 1;
        }

        return counts;
    }
}

