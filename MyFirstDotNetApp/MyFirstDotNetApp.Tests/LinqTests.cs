using System.Collections.Generic;
using System.Linq;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class LinqTests
    {
        [TestMethod]
        public void FilterCollection_WithLINQ_ReturnsFilteredResults()
        {
            // Arrange
            var numbers = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();

            // Assert
            Assert.AreEqual(2, evenNumbers.Count);
            Assert.IsTrue(evenNumbers.Contains(2) && evenNumbers.Contains(4));
        }

        [TestMethod]
        public void ProjectCollection_WithLINQ_ReturnsProjectedResults()
        {
            // Arrange
            var names = new List<string> { "Alice", "Bob", "Charlie" };

            // Act
            var upperCaseNames = names.Select(name => name.ToUpper()).ToList();

            // Assert
            CollectionAssert.AreEqual(
                new List<string> { "ALICE", "BOB", "CHARLIE" },
                upperCaseNames
            );
        }

        [TestMethod]
        public void AggregateCollection_WithLINQ_ReturnsAggregatedResult()
        {
            // Arrange
            var numbers = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var sum = numbers.Sum();

            // Assert
            Assert.AreEqual(15, sum);
        }
    }
}
