namespace Io;

public static class Csv
{
    public static IReadOnlyList<string[]> Read(TextReader reader)
    {
        var rows = new List<string[]>();
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            rows.Add(line.Split(','));
        }

        return rows;
    }

    public static void Write(TextWriter writer, IEnumerable<string[]> rows)
    {
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",", row));
        }
    }
}

