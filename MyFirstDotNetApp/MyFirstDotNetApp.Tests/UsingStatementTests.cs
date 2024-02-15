using System.IO;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class UsingStatementTests
    {
        private string _testFilePath = Path.Combine(Path.GetTempPath(), "usingTest.txt");

        [TestCleanup]
        public void TestCleanup()
        {
            // Delete the temporary file after each test
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [TestMethod]
        public void UsingStatement_DisposesFileStream_Correctly()
        {
            // Arrange
            // Ensure file exists for testing
            File.WriteAllText(_testFilePath, "Test");

            // Act
            using (var fileStream = File.Open(_testFilePath, FileMode.Open))
            {
                // Perform file operations
            } // FileStream is disposed here

            // Assert
            // Asserting disposal indirectly by checking if the file can be opened again immediately
            bool isOpenedSuccessfully = false;
            try
            {
                using (var testStream = File.Open(_testFilePath, FileMode.Open))
                {
                    isOpenedSuccessfully = true;
                }
            }
            catch (IOException)
            {
                isOpenedSuccessfully = false;
            }

            Assert.IsTrue(
                isOpenedSuccessfully,
                "The file stream was not properly disposed, as the file cannot be opened again."
            );
        }

        [TestMethod]
        public void UsingDeclaration_DisposesObjectAtEndOfScope_Correctly()
        {
            // Arrange
            CreateTemporaryFile();

            // Act
            { // Create a block scope
                using var fileStream = File.Open(_testFilePath, FileMode.Open);
                // File operations
            } // FileStream is disposed here

            // Assert
            // Asserting disposal indirectly by checking if the file can be opened again immediately
            bool isOpenedSuccessfully = false;
            try
            {
                using (var testStream = File.Open(_testFilePath, FileMode.Open))
                {
                    isOpenedSuccessfully = true;
                }
            }
            catch (IOException)
            {
                isOpenedSuccessfully = false;
            }

            Assert.IsTrue(
                isOpenedSuccessfully,
                "The file stream was not properly disposed, as the file cannot be opened again."
            );
        }

        private void CreateTemporaryFile()
        {
            // Ensure the directory exists
            var tempDirectory = Path.GetTempPath();
            var tempFilePath = Path.Combine(tempDirectory, "usingTest.txt");

            // Set the class-level _testFilePath to the new temp file's path
            _testFilePath = tempFilePath;

            // Check if the file already exists to avoid overwriting it unintentionally
            if (File.Exists(tempFilePath))
            {
                // Optional: Delete the existing file if you want a fresh start for each test
                File.Delete(tempFilePath);
            }

            // Create the file with some initial content
            File.WriteAllText(tempFilePath, "Initial content for testing.");
        }
    }
}
