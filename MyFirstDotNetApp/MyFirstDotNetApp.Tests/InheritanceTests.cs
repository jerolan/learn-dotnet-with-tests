namespace MyFirstDotNetApp.Tests;

[TestClass]
public class InheritanceTests
{
    [TestMethod]
    public void AnimalSpeak_Polymorphism_CallsCorrectMethod()
    {
        // Arrange
        Animal animal = new Dog(); // Dog inherits from Animal

        // Act
        var sound = animal.Speak();

        // Assert
        Assert.AreEqual("Bark", sound);
    }
}