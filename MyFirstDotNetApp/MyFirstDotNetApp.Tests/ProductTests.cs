namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class ProductTests
    {
        [TestMethod]
        public void ProductName_PropertyWorksAsExpected()
        {
            // Arrange
            var product = new Product();
            var expectedName = "Gadget";

            // Act
            product.Name = expectedName; // Testing set
            var actualName = product.Name; // Testing get

            // Assert
            Assert.AreEqual(expectedName, actualName);
        }

        [TestMethod]
        public void CalculateDiscount_PriceAboveThreshold_AppliesDiscount()
        {
            // Arrange
            var product = new Product { Price = 200.00m };
            var expectedPriceAfterDiscount = 180.00m; // Assuming a 10% discount

            // Act
            product.ApplyDiscount(10); // Apply a 10% discount

            // Assert
            Assert.AreEqual(expectedPriceAfterDiscount, product.Price);
        }
    }
}
