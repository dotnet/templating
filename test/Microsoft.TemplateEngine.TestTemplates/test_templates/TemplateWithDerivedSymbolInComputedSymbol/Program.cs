namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if (IsWillSmith)
            Console.WriteLine("Welcome, User_Name.");
#else
            Console.WriteLine("Unknown user: User_Name.");
#endif
        }
    }
}