namespace Decimal;
public sealed record Money(decimal Amount)
{
    public Money Add(Money other) => new(Amount + other.Amount);
    public Money Multiply(int quantity) => new(Amount * quantity);
    public Money Rounded(int places = 2) => new(Math.Round(Amount, places, MidpointRounding.AwayFromZero));
}
