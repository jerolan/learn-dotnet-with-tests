namespace OrderSummary;

public sealed record OrderLine(string Product, int Quantity, decimal UnitPrice);

public static class OrderFormatter
{
    public static string Format(IEnumerable<OrderLine> lines)
    {
        var rows = lines
            .Select(line => $"{line.Product} x{line.Quantity} @ {line.UnitPrice:0.00}")
            .ToList();

        var total = lines.Sum(line => line.Quantity * line.UnitPrice);

        rows.Add($"Total: {total:0.00}");

        return string.Join(Environment.NewLine, rows);
    }
}

