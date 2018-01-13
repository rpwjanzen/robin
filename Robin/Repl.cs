using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robin
{
    public class Repl
    {
        const string Prompt = ">> ";

        public static void Start(TextReader reader, TextWriter writer)
        {
            writer.Write(Prompt);
            var line = reader.ReadLine();
            while(line != null)
            {
                var lexer = new Lexer(line);
                var parser = new Parser(lexer);
                var program = parser.ParseProgram();
                if (parser.Errors.Count != 0)
                {
                    PrintErrors(writer, parser.Errors);
                }
                else
                {
                    writer.WriteLine(program);
                }

                writer.Write(Prompt);
                line = reader.ReadLine();
            }
        }

        private static void PrintErrors(TextWriter writer, List<string> errors)
        {
            const string monkeyFace = "<<<monkey face here>>>";

            writer.WriteLine(monkeyFace);
            writer.WriteLine("Woops! We ran into some monkey buisiness here!");
            writer.WriteLine(" parser errors:");
            foreach (var error in errors)
            {
                writer.WriteLine($"\t{error}");
            }
        }
    }
}
