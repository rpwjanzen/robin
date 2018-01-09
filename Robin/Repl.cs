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
                var token = lexer.NextToken();
                while(token.Type != TokenType.Eof)
                {
                    writer.WriteLine(token);
                    token = lexer.NextToken();
                }

                writer.Write(Prompt);
                line = reader.ReadLine();
            }
        }
    }
}
