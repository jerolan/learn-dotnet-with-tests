namespace MyFirstDotNetApp;

public class ObjectModifier
{
    public static void ChangeReference(ref SimpleObject obj, SimpleObject newObj)
    {
        obj = newObj;
    }
}

public class SimpleObject
{
    public string Value { get; set; }
}