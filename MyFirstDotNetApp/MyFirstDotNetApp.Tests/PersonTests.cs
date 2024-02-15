namespace MyFirstDotNetApp.Tests;

[TestClass]
public class PersonTests
{
    [TestMethod]
    public void CreatingPerson_SetsNameAndAgeProperties_Correctly()
    {
        // Arrange & Act
        var person = new Person { Name = "John Doe", Age = 30 };

        // Assert
        Assert.AreEqual("John Doe", person.Name);
        Assert.AreEqual(30, person.Age);
    }

    [TestMethod]
    public void IntroduceYourself_ReturnsCorrectIntroduction()
    {
        // Arrange
        var person = new Person { Name = "Jane Doe", Age = 25 };

        // Act
        var introduction = person.IntroduceYourself();

        // Assert
        Assert.AreEqual("Hello, my name is Jane Doe and I am 25 years old.", introduction);
    }
}