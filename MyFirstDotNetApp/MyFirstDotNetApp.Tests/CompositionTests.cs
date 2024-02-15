namespace MyFirstDotNetApp.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using MyFirstDotNetApp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CompositionTests
    {
        [TestMethod]
        public void RobotPerformsTasks_ThroughComposition_HasExpectedBehavior()
        {
            // Arrange
            var robot = new Robot(new List<IRobotTask> { new SpeakTask(), new MoveTask() });

            // Act
            var speakResult = robot.PerformTask("Speak");
            var moveResult = robot.PerformTask("Move");

            // Assert
            Assert.AreEqual("Speaking", speakResult);
            Assert.AreEqual("Moving", moveResult);
        }
    }
}
