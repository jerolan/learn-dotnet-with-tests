using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Classes.Tests;

[TestClass]
public sealed class BankAccountTests
{
    [TestMethod]
    public void NewAccount_HasTheInitialBalance()
    {
        var account = new BankAccount(100m);

        Assert.AreEqual(100m, account.Balance);
    }

    [TestMethod]
    public void Deposit_AddsToTheBalance()
    {
        var account = new BankAccount(100m);

        account.Deposit(50m);

        Assert.AreEqual(150m, account.Balance);
    }

    [TestMethod]
    public void Withdraw_SubtractsFromTheBalance()
    {
        var account = new BankAccount(100m);

        account.Withdraw(30m);

        Assert.AreEqual(70m, account.Balance);
    }

    [TestMethod]
    public void Withdraw_MoreThanTheBalance_ThrowsAndKeepsTheBalance()
    {
        var account = new BankAccount(100m);

        Assert.ThrowsException<InvalidOperationException>(() => account.Withdraw(150m));

        Assert.AreEqual(100m, account.Balance);
    }

    [TestMethod]
    public void TwoVariables_CanPointToTheSameAccount()
    {
        var account = new BankAccount(100m);
        var alias = account;

        alias.Deposit(50m);

        Assert.AreEqual(150m, account.Balance);
    }
}

