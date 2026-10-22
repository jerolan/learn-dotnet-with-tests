using Bogus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shop;

namespace Shop.Tests;

[TestClass]
public sealed class OrderValidatorTests
{
    [TestMethod]
    public void ValidGeneratedOrdersPass()
    {
        var orders = ValidOrders(seed: 1234);

        var validator = new OrderValidator();

        foreach (var order in orders)
        {
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                validator.Validate(order).ToArray(),
                order.ToString());
        }
    }

    [TestMethod]
    public void SameSeedProducesTheSameOrders()
    {
        var first = ValidOrders(seed: 1234);
        var second = ValidOrders(seed: 1234);

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void OrderWithoutEmailIsRejected()
    {
        var order = new Order(
            "Ana Torres",
            "ana.example.com",
            12.50m,
            DateTime.UtcNow.AddMinutes(-1));

        var errors = new OrderValidator().Validate(order);

        CollectionAssert.Contains(errors.ToList(), "Email is invalid.");
    }

    [TestMethod]
    public void OrderPlacedInTheFutureIsRejected()
    {
        var order = new Order(
            "Ana Torres",
            "ana@example.com",
            12.50m,
            DateTime.UtcNow.AddDays(1));

        var errors = new OrderValidator().Validate(order);

        CollectionAssert.Contains(errors.ToList(), "Placed date cannot be in the future.");
    }

    private static List<Order> ValidOrders(int seed)
    {
        return new Faker<Order>()
            .UseSeed(seed)
            .CustomInstantiator(f => new Order(
                f.Name.FullName(),
                f.Internet.Email(),
                f.Random.Decimal(1m, 500m),
                f.Date.Between(
                    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc))))
            .Generate(100);
    }
}
