namespace MyFirstDotNetApp.Tests;

[TestClass]
public class DecimalTests
{
    [TestMethod]
    public void DecimalAddition_ReturnsCorrectResult()
    {
        // Arrange
        var numberOne = 5.5m;
        var numberTwo = 2.5m;

        // Act
        var sum = numberOne + numberTwo;

        // Assert
        Assert.AreEqual(8.0m, sum);
    }

    [TestMethod]
    public void DecimalMultiplication_HighPrecision_ReturnsAccurateResult()
    {
        // Arrange
        var price = 19.95m;
        var quantity = 3m;

        // Act
        var total = price * quantity;

        // Assert
        Assert.AreEqual(59.85m, total);
    }

    [TestMethod]
    public void DecimalRounding_RoundsToTwoDecimalPlaces_CorrectlyRoundsUp()
    {
        // Arrange
        var originalValue = 10.555m;

        // Act
        var roundedValue = Math.Round(originalValue, 2);

        // Assert
        Assert.AreEqual(10.56m, roundedValue);
    }
}