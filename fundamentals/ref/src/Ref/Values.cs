namespace Ref;

public static class Values
{
    public static void Swap(ref int left, ref int right)
    {
        var temp = left;
        left = right;
        right = temp;
    }

    public static bool TryDivide(int dividend, int divisor, out int quotient)
    {
        if (divisor == 0)
        {
            quotient = 0;
            return false;
        }

        quotient = dividend / divisor;
        return true;
    }

    public static ref int At(int[] values, int index)
    {
        return ref values[index];
    }
}

public readonly struct Point
{
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public int ManhattanDistance(in Point other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }
}

