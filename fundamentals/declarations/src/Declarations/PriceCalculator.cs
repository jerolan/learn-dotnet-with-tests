namespace Declarations;

public sealed class PriceCalculator
{
    public const decimal DefaultVatRate = 0.16m;

    private readonly decimal _vatRate;

    public PriceCalculator()
        : this(DefaultVatRate)
    {
    }

    public PriceCalculator(decimal vatRate)
    {
        _vatRate = vatRate;
    }

    public decimal TotalWithVat(decimal price)
    {
        var vat = price * _vatRate;
        return price + vat;
    }
}

