using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robin
{
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
