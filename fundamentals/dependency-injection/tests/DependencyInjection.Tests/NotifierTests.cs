using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DependencyInjection.Tests;

[TestClass]
public sealed class NotifierTests
{
    [TestMethod]
    public void Notify_SendsTheFormattedMessage()
    {
        var sender = new RecordingSender();
        var notifier = new Notifier(new IMessageSender[] { sender });

        notifier.Notify("Disk full");

        Assert.AreEqual(1, sender.Messages.Count);
        Assert.AreEqual("[Alert] Disk full", sender.Messages[0]);
    }

    [TestMethod]
    public void Notify_SendsToEverySender()
    {
        var email = new RecordingSender();
        var sms = new RecordingSender();
        var notifier = new Notifier(new IMessageSender[] { email, sms });

        notifier.Notify("Disk full");

        Assert.AreEqual("[Alert] Disk full", email.Messages[0]);
        Assert.AreEqual("[Alert] Disk full", sms.Messages[0]);
    }

    private sealed class RecordingSender : IMessageSender
    {
        public List<string> Messages { get; } = new List<string>();

        public void Send(string message)
        {
            Messages.Add(message);
        }
    }
}

