namespace MyFirstDotNetApp;

public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }

    public void ApplyDiscount(decimal percentage)
    {
        var discountAmount = Price * (percentage / 100);
        Price -= discountAmount;
    }
}