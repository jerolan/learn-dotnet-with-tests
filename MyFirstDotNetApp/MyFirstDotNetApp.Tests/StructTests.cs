namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class StructTests
    {
        [TestMethod]
        public void StructAssignment_CreatesCopy()
        {
            // Arrange
            var originalStruct = new SimpleStruct { Number = 1, Text = "Original" };
            var copiedStruct = originalStruct; // This creates a copy

            // Act
            copiedStruct.Number = 2; // Modifying the copy shouldn't affect the original

            // Assert
            Assert.AreEqual(1, originalStruct.Number); // Original should remain unchanged
        }
    }
}
