namespace MyFirstDotNetApp;

public interface IPaymentGateway
{
    bool ProcessPayment(decimal amount);
}