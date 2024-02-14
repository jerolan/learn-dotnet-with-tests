namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class IntegerTests
    {
        [TestMethod]
        public void IntegerAddition_ReturnsCorrectResult()
        {
            // Arrange
            int numberOne = 5;
            int numberTwo = 3;

            // Act
            int sum = numberOne + numberTwo;

            // Assert
            Assert.AreEqual(8, sum);
        }

        [TestMethod]
        public void FloatingPointDivision_ReturnsApproximateResult()
        {
            // Arrange
            double numerator = 10;
            double denominator = 3;

            // Act
            double result = numerator / denominator;

            // Assert
            Assert.AreEqual(3.33, result, 0.01); // Allow a small margin of error
        }

        [TestMethod]
        public void BooleanLogic_AndOperator_ReturnsTrueOnlyIfBothTrue()
        {
            // Arrange
            bool firstCondition = true;
            bool secondCondition = false;

            // Act
            bool result = firstCondition && secondCondition;

            // Assert
            Assert.IsFalse(result);
        }
    }
}
