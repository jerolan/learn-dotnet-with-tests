using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UsingKeyword.Tests;

[TestClass]
public sealed class SessionTests
{
    [TestMethod]
    public void Run_DisposesTheSession()
    {
        var log = new List<string>();
        Session? captured = null;

        SessionRunner.Run(log, session => captured = session);

        Assert.IsTrue(captured!.IsDisposed);
        CollectionAssert.AreEqual(new[] { "open", "dispose" }, log);
    }

    [TestMethod]
    public void Run_DisposesWhenTheActionThrows()
    {
        var log = new List<string>();

        Assert.ThrowsException<InvalidOperationException>(() =>
            SessionRunner.Run(log, _ => throw new InvalidOperationException()));

        CollectionAssert.AreEqual(new[] { "open", "dispose" }, log);
    }

    [TestMethod]
    public void UsingStatement_DisposesAtTheEndOfTheBlock()
    {
        var log = new List<string>();
        Session? captured;

        using (var session = new Session(log))
        {
            captured = session;
            Assert.IsFalse(session.IsDisposed);
        }

        Assert.IsTrue(captured!.IsDisposed);
    }

    [TestMethod]
    public void Dispose_IsIdempotent()
    {
        var log = new List<string>();
        var session = new Session(log);

        session.Dispose();
        session.Dispose();

        CollectionAssert.AreEqual(new[] { "open", "dispose" }, log);
    }
}

