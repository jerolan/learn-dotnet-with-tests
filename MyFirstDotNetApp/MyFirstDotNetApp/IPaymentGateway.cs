public interface IPaymentGateway
{
    bool ProcessPayment(decimal amount);
}
