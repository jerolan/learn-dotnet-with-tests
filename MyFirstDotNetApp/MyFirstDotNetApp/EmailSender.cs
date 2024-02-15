namespace MyFirstDotNetApp;

public class EmailSender : IMessageSender
{
    public string SendMessage(string message)
    {
        // Simulate sending an email
        return $"Email sent with message: {message}";
    }
}