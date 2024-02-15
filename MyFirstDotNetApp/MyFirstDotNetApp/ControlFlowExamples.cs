namespace MyFirstDotNetApp
{
    public class ControlFlowExamples
    {
        public static string ReturnEarlyIfTrue(bool condition)
        {
            if (condition)
            {
                return "Returned Early";
            }

            return "Returned at End";
        }
    }
}
