namespace MyFirstDotNetApp;

public class PaymentService
{
    private readonly IPaymentGateway _paymentGateway;

    public PaymentService(IPaymentGateway paymentGateway)
    {
        _paymentGateway = paymentGateway;
    }

    public bool MakePayment(decimal amount)
    {
        return _paymentGateway.ProcessPayment(amount);
    }
}