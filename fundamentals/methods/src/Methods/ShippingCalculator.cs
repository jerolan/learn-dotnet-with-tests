namespace Methods;

public class ShippingCalculator
{
    private const decimal ExpressSurcharge = 20m;

    public decimal RatePerKg { get; set; } = 5m;

    public decimal Cost(decimal weightKg, bool express = false)
    {
        var cost = weightKg * RatePerKg;

        if (express)
        {
            cost += ExpressSurcharge;
        }

        return cost;
    }
}

