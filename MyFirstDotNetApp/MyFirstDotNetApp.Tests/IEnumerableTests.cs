using System.Collections.Generic;
using System.Linq;

namespace MyFirstDotNetApp.Tests;

[TestClass]
public class IEnumerableTests
{
    [TestMethod]
    public void IEnumerable_IterateCollection_ReturnsCorrectSequence()
    {
        // Arrange
        IEnumerable<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var sum = 0;
        foreach (var number in numbers) sum += number;

        // Assert
        Assert.AreEqual(15, sum); // Sum of 1 through 5
    }

    [TestMethod]
    public void IEnumerable_FilterCollection_ReturnsFilteredResults()
    {
        // Arrange
        IEnumerable<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();

        // Assert
        Assert.AreEqual(2, evenNumbers.Count); // Expect 2 even numbers
        CollectionAssert.AreEqual(new List<int> { 2, 4 }, evenNumbers);
    }
}