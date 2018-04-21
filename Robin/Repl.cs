namespace Robin
{
    using System.IO;
    using System.Collections.Generic;
    using Robin.Lexing;
    using Robin.Parsing;

    public class Repl
    {
        const string Prompt = ">> ";

        public static void Start(TextReader reader, TextWriter writer)
        {
            var env = new Environment();
            var macroEnv = new Environment();
            var evaluator = new Eval.Evaluator();

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

                    writer.Write(Prompt);
                    line = reader.ReadLine();
                    continue;
                }

                evaluator.DefineMacros(program, macroEnv);
                var expanded = evaluator.ExpandMacros(program, macroEnv);
                var evaluated = evaluator.Eval(expanded, env);
                if (evaluated != null)
                {
                    writer.WriteLine(evaluated.Inspect());
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
