namespace MyFirstDotNetApp.Tests;

[TestClass]
public class FileIOTests
{
    private readonly string _testFilePath = Path.Combine(Path.GetTempPath(), "testfile.txt");

    [TestInitialize]
    public void Initialize()
    {
        if (File.Exists(_testFilePath)) File.Delete(_testFilePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_testFilePath)) File.Delete(_testFilePath);
    }

    [TestMethod]
    public async Task WriteToFile_FileContainsExpectedContent()
    {
        // Arrange
        var expectedContent = "Hello, File IO";

        // Act
        await File.WriteAllTextAsync(_testFilePath, expectedContent);

        // Assert
        var fileContent = await File.ReadAllTextAsync(_testFilePath);
        Assert.AreEqual(expectedContent, fileContent);
    }
}