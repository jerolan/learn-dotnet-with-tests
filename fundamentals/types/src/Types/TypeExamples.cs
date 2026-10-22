namespace Types;

public static class TypeExamples
{
    public static int Add(int a, int b)
    {
        return a + b;
    }

    public static bool IsEven(int value)
    {
        return value % 2 == 0;
    }

    public static string Describe(object value)
    {
        return value switch
        {
            int i => string.Format("int:{0}", i),
            double d => string.Format("double:{0}", d),
            bool b => string.Format("bool:{0}", b),
            _ => "unknown"
        };
    }
}
