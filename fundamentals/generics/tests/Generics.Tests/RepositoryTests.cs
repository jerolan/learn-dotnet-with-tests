using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Generics.Tests;

[TestClass]
public sealed class RepositoryTests
{
    [TestMethod]
    public void AddThenGet_ReturnsTheItem()
    {
        var repository = new Repository<Product>();
        var product = new Product(1, "Keyboard");

        repository.Add(product);

        Assert.AreEqual(product, repository.Get(1));
    }

    [TestMethod]
    public void GetMissing_ReturnsNull()
    {
        var repository = new Repository<Product>();

        Assert.IsNull(repository.Get(99));
    }

    [TestMethod]
    public void All_ReturnsEveryItem()
    {
        var repository = new Repository<Product>();
        repository.Add(new Product(1, "Keyboard"));
        repository.Add(new Product(2, "Mouse"));

        var all = new List<Product>(repository.All);

        Assert.AreEqual(2, all.Count);
    }

    [TestMethod]
    public void WorksWithAnotherEntityType()
    {
        var repository = new Repository<Customer>();
        repository.Add(new Customer(7, "Ada"));

        Assert.AreEqual("Ada", repository.Get(7)!.Name);
    }
}

