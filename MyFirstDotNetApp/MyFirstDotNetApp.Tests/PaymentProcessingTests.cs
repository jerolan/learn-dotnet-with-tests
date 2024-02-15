using Moq;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class PaymentProcessingTests
    {
        [TestMethod]
        public void MakePayment_WithMockPaymentGateway_ProcessesPaymentSuccessfully()
        {
            // Arrange
            var mockPaymentGateway = new Mock<IPaymentGateway>();
            mockPaymentGateway
                .Setup(gateway => gateway.ProcessPayment(It.IsAny<decimal>()))
                .Returns(true);

            var paymentService = new PaymentService(mockPaymentGateway.Object);
            var paymentAmount = 100m;

            // Act
            var result = paymentService.MakePayment(paymentAmount);

            // Assert
            Assert.IsTrue(result);
            mockPaymentGateway.Verify(
                gateway => gateway.ProcessPayment(paymentAmount),
                Times.Once()
            );
        }
    }
}
