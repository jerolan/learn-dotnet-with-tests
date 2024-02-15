namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class ControlFlowTests
    {
        [TestMethod]
        public void IfStatement_ExecutesTrueBranch_Correctly()
        {
            // Arrange
            bool condition = true;
            string result = "";

            // Act
            if (condition)
            {
                result = "True Branch";
            }
            else
            {
                result = "False Branch";
            }

            // Assert
            Assert.AreEqual("True Branch", result);
        }

        [TestMethod]
        public void ForLoop_ExecutesMultipleTimes_Correctly()
        {
            // Arrange
            int sum = 0;

            // Act
            for (int i = 1; i <= 5; i++)
            {
                sum += i;
            }

            // Assert
            Assert.AreEqual(15, sum);
        }

        [TestMethod]
        public void ReturnStatement_ReturnsEarlyIfConditionIsTrue()
        {
            // Arrange & Act
            var resultTrueCondition = ControlFlowExamples.ReturnEarlyIfTrue(true);
            var resultFalseCondition = ControlFlowExamples.ReturnEarlyIfTrue(false);

            // Assert
            Assert.AreEqual("Returned Early", resultTrueCondition);
            Assert.AreEqual("Returned at End", resultFalseCondition);
        }
    }
}
