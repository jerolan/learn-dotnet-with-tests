namespace MyFirstDotNetApp.Tests;

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

    [TestMethod]
    public void WithKeyword_CreatesModifiedCopy()
    {
        // Arrange
        var originalRecord = new PersonRecord("John", 30);

        // Act
        var modifiedRecord = originalRecord with
        {
            Age = 31
        };

        // Assert
        Assert.AreEqual("John", originalRecord.Name); // Original remains unchanged
        Assert.AreEqual(30, originalRecord.Age); // Original remains unchanged
        Assert.AreEqual("John", modifiedRecord.Name); // Name is copied
        Assert.AreEqual(31, modifiedRecord.Age); // Age is modified
    }
}