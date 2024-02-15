using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class RefTests
    {
        [TestMethod]
        public void ModifyValue_WithRef_UpdatesValue()
        {
            // Arrange
            int originalValue = 10;
            int expectedValue = 20;

            // Act
            Modifier.DoubleValue(ref originalValue);

            // Assert
            Assert.AreEqual(expectedValue, originalValue);
        }

        [TestMethod]
        public void ModifyObjectReference_WithRef_ChangesObjectReference()
        {
            // Arrange
            var originalObject = new SimpleObject { Value = "Original" };
            var expectedNewObject = new SimpleObject { Value = "Modified" };

            // Act
            ObjectModifier.ChangeReference(ref originalObject, expectedNewObject);

            // Assert
            Assert.AreSame(expectedNewObject, originalObject);
        }
    }
}
