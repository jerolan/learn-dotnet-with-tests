namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class RecordTests
    {
        [TestMethod]
        public void RecordEquality_ByValue()
        {
            // Arrange
            var record1 = new SimpleRecord("Name", 1);
            var record2 = new SimpleRecord("Name", 1);

            // Act & Assert
            Assert.AreEqual(record1, record2); // Records are equal if their values are equal
        }
    }
}
