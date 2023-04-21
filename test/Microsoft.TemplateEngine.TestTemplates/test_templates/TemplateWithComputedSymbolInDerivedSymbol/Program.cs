namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if (IsWillSmith)
            Console.WriteLine("Welcome, Will Smith.");
#else
            Console.WriteLine("Unknown user.");
#endif
            Console.WriteLine("This is Will Smith: Assertion.");
        }
    }
}