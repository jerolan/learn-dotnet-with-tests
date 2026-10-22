namespace Classes;

public class BankAccount
{
    public BankAccount(decimal initialBalance)
    {
        Balance = initialBalance;
    }

    public decimal Balance { get; private set; }

    public void Deposit(decimal amount)
    {
        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        if (amount > Balance)
        {
            throw new InvalidOperationException("Insufficient funds.");
        }

        Balance -= amount;
    }
}

