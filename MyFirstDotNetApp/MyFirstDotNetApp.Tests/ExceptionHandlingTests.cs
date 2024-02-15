namespace MyFirstDotNetApp.Tests;

[TestClass]
public class ExceptionHandlingTests
{
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Method_ThrowsInvalidOperationException()
    {
        // Arrange
        var problematicMethod = new ProblematicClass();

        // Act
        problematicMethod.RunOperationThatFails();

        // Assert is handled by the ExpectedException attribute
    }

    [TestMethod]
    public void TryCatch_CatchesExpectedExceptionType()
    {
        // Arrange
        var hasCaughtExpectedException = false;
        var problematicMethod = new ProblematicClass();

        // Act
        try
        {
            problematicMethod.RunOperationThatFails();
        }
        catch (InvalidOperationException)
        {
            hasCaughtExpectedException = true;
        }

        // Assert
        Assert.IsTrue(hasCaughtExpectedException);
    }
}

public class ProblematicClass
{
    public void RunOperationThatFails()
    {
        throw new InvalidOperationException("Operation Failed.");
    }
}
