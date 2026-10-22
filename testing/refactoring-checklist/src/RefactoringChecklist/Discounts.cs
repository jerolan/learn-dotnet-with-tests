namespace RefactoringChecklist;

public static class Discounts
{
    private const decimal MemberDiscount = 0.05m;

    public static decimal Apply(decimal price, int percent, bool member)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent));
        }

        var afterPercent = price * (1 - percent / 100m);

        return member
            ? afterPercent * (1 - MemberDiscount)
            : afterPercent;
    }
}

