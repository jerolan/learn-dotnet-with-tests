using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class VariableTests
    {
        [TestMethod]
        public void VariableHoldsExpectedValue()
        {
            // Arrange
            int number = 5;

            // Act & Assert
            Assert.AreEqual(5, number);
        }

        [TestMethod]
        public void ConstantHoldsExpectedValue()
        {
            // Arrange
            const string greeting = "Hello, World!";

            // Act & Assert
            Assert.AreEqual("Hello, World!", greeting);
        }

        [TestMethod]
        public void VarInfersCorrectType()
        {
            // Arrange
            var number = 5; // Should infer int
            var word = "test"; // Should infer string

            // Act & Assert
            Assert.IsTrue(number is int);
            Assert.IsTrue(word is string);
        }
    }
}
