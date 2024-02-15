using System.Collections.Generic;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class GenericsTests
    {
        [TestMethod]
        public void GenericRepository_AddAndGetItem_ItemIsCorrectlyManaged()
        {
            // Arrange
            var repository = new GenericRepository<string>();
            var item = "Hello, Generics";

            // Act
            repository.Add(item);
            var retrievedItem = repository.GetById(0);

            // Assert
            Assert.AreEqual(item, retrievedItem);
        }

        [TestMethod]
        public void Swap_SwapsIntValues_ValuesAreSwapped()
        {
            // Arrange
            int a = 1,
                b = 2;

            // Act
            Utility.Swap(ref a, ref b);

            // Assert
            Assert.AreEqual(2, a);
            Assert.AreEqual(1, b);
        }
    }
}
