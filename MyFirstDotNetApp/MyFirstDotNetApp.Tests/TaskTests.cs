using System.Threading.Tasks;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class TaskTests
    {
        [TestMethod]
        public async Task Task_ExecutesToCompletion_CompletesSuccessfully()
        {
            // Arrange
            var task =
                Task.Run(() => { /* Simulate work */
                });

            // Act & Assert
            await task; // Awaiting completion of the task
            Assert.IsTrue(task.IsCompleted); // Verify task is completed
        }

        [TestMethod]
        public async Task TaskWithResult_ReturnsValue_ValueIsCorrect()
        {
            // Arrange
            var valueTask = Task.Run(() => "Hello, Task");

            // Act
            var result = await valueTask;

            // Assert
            Assert.AreEqual("Hello, Task", result);
        }

        [TestMethod]
        public async Task Task_ThrowsException_IsHandledCorrectly()
        {
            // Arrange
            var exceptionTask = Task.Run(() =>
            {
                throw new InvalidOperationException();
            });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => exceptionTask);
        }
    }
}
