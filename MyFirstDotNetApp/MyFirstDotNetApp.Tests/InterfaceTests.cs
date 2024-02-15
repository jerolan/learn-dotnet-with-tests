namespace MyFirstDotNetApp.Tests;

[TestClass]
public class InterfaceTests
{
    [TestMethod]
    public void SendMessage_UsingIMessageSender_SendsCorrectMessage()
    {
        // Arrange
        IMessageSender sender = new EmailSender(); // EmailSender implements IMessageSender

        // Act
        var result = sender.SendMessage("Hello, World!");

        // Assert
        Assert.IsTrue(result.Contains("Email sent"));
    }
}