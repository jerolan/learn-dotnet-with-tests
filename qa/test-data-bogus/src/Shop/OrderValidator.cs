namespace Shop;

public sealed class OrderValidator
{
    public IReadOnlyList<string> Validate(Order order)
    {
        var errors = new List<string>();

        if (!order.Email.Contains('@'))
        {
            errors.Add("Email is invalid.");
        }

        if (order.PlacedAt > DateTime.UtcNow)
        {
            errors.Add("Placed date cannot be in the future.");
        }

        return errors;
    }
}
