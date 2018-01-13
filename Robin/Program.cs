namespace Robin.App
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            var currentUser = "User";

            Console.WriteLine("Hello " + currentUser + ".");
            Repl.Start(Console.In, Console.Out);
        }
    }
}
