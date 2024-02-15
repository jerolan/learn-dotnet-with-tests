using System.Linq;
using Moq;
using System.Collections.Generic;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class IQueryableTests
    {
        [TestMethod]
        public void IQueryable_QueryExecutedOnDataSource_ReturnsExpectedResults()
        {
            // Arrange
            var data = new List<Person>
            {
                new Person { Name = "Alice", Age = 30 },
                new Person { Name = "Bob", Age = 25 },
                new Person { Name = "Charlie", Age = 35 }
            }.AsQueryable();

            var queryableData = new Mock<IQueryable<Person>>();
            queryableData.Setup(m => m.Provider).Returns(data.Provider);
            queryableData.Setup(m => m.Expression).Returns(data.Expression);
            queryableData.Setup(m => m.ElementType).Returns(data.ElementType);
            queryableData.Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            // Act
            var results = queryableData.Object.Where(p => p.Age > 30).ToList();

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Charlie", results.First().Name);
        }
    }
}
