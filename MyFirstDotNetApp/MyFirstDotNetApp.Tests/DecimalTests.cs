namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class DecimalTests
    {
        [TestMethod]
        public void DecimalAddition_ReturnsCorrectResult()
        {
            // Arrange
            decimal numberOne = 5.5m;
            decimal numberTwo = 2.5m;

            // Act
            decimal sum = numberOne + numberTwo;

            // Assert
            Assert.AreEqual(8.0m, sum);
        }

        [TestMethod]
        public void DecimalMultiplication_HighPrecision_ReturnsAccurateResult()
        {
            // Arrange
            decimal price = 19.95m;
            decimal quantity = 3m;

            // Act
            decimal total = price * quantity;

            // Assert
            Assert.AreEqual(59.85m, total);
        }

        [TestMethod]
        public void DecimalRounding_RoundsToTwoDecimalPlaces_CorrectlyRoundsUp()
        {
            // Arrange
            decimal originalValue = 10.555m;

            // Act
            decimal roundedValue = Math.Round(originalValue, 2);

            // Assert
            Assert.AreEqual(10.56m, roundedValue);
        }
    }
}
