namespace MyFirstDotNetApp.Tests
{
    using System.Collections.Generic;

    [TestClass]
    public class CollectionsTests
    {
        [TestMethod]
        public void List_AddAndRetrieveItems_ItemsAreCorrectlyManaged()
        {
            // Arrange
            var names = new List<string>();

            // Act
            names.Add("Alice");
            names.Add("Bob");

            // Assert
            Assert.AreEqual(2, names.Count);
            Assert.AreEqual("Alice", names[0]);
            Assert.AreEqual("Bob", names[1]);
        }

        [TestMethod]
        public void Dictionary_AddAndRetrieveByKey_ValueIsCorrect()
        {
            // Arrange
            var capitals = new Dictionary<string, string>();

            // Act
            capitals.Add("France", "Paris");
            capitals.Add("Japan", "Tokyo");

            // Assert
            Assert.AreEqual("Paris", capitals["France"]);
            Assert.AreEqual("Tokyo", capitals["Japan"]);
        }

        public void Queue_EnqueueAndDequeue_ItemsAreCorrectlyProcessed()
        {
            // Arrange
            var queue = new Queue<int>();

            // Act
            queue.Enqueue(1);
            queue.Enqueue(2);
            int firstOut = queue.Dequeue();

            // Assert
            Assert.AreEqual(1, firstOut); // FIFO order
            Assert.AreEqual(1, queue.Count); // One item should be left
        }

        [TestMethod]
        public void Stack_PushAndPop_ItemsAreCorrectlyProcessed()
        {
            // Arrange
            var stack = new Stack<int>();

            // Act
            stack.Push(1);
            stack.Push(2);
            int lastOut = stack.Pop();

            // Assert
            Assert.AreEqual(2, lastOut); // LIFO order
            Assert.AreEqual(1, stack.Count); // One item should be left
        }
    }
}
