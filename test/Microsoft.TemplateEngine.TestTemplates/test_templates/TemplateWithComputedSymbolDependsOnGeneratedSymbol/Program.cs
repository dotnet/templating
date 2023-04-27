namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if (UseCustomLogic)
            Console.WriteLine("Start custom logic.");
#else
            Console.WriteLine("No custom logic is needed.");
#endif
        }
    }
}