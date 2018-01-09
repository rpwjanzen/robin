using NUnit.Framework;
using Robin;
using System;
using System.Diagnostics;
using System.Linq;

namespace RobinTests
{
    [TestFixture]
    public class ParserTest
    {
        [TestCase("return 5;")]
        [TestCase("return 5 + 6;")]
        [TestCase("return -1;")]
        [TestCase("return 0;")]
        [TestCase("return a;")]
        [TestCase("return (x - y + z);")]
        [TestCase("return x - y - z;")]
        public void Return_statement(string text)
        {
            var parser = new Parser(new Lexer(text));
            var program = parser.ParseProgram();

            Assert.That(program.Statements[0], Is.AssignableFrom<ReturnStatement>());
        }

        [TestCase("let x = 5;")]
        [TestCase("let y = 10;")]
        public void Let_expression_parses_as_let_expression(string text)
        {
            var parser = new Parser(new Lexer(text));
            var program = parser.ParseProgram();

            Assert.That(program.Statements[0], Is.AssignableFrom<LetStatement>());
            AssertNoErrors(parser);
        }

        [TestCase("let x = 5;", "x")]
        [TestCase("let y = 5;", "y")]
        [TestCase("let add = 5;", "add")]
        public void Single_assignment_let_expression_parses(string text, string identifierLiteral)
        {
            var parser = new Parser(new Lexer(text));
            var program = parser.ParseProgram();
            var letStatement = (LetStatement)program.Statements[0];

            Assert.That(letStatement.Token, Is.EqualTo(new Token(TokenType.Let, "let")));
            var identifier = (Identifier)letStatement.Name;
            Assert.That(identifier.Token, Is.EqualTo(new Token(TokenType.Ident, identifierLiteral)));
            // FIXME
            Assert.That(letStatement.Value, Is.Null);
            AssertNoErrors(parser);
        }

        [TestCase("omeIdentifier")]
        [TestCase("identifier")]
        [TestCase("Identifier")]
        public void Simple_identifier_parse(string text)
        {
            var parser = new Parser(new Lexer(text));
            var program = parser.ParseProgram();
            var expressionStatement = (ExpressionStatement)program.Statements[0];
            var identifierStatement = (Identifier)expressionStatement.Expression;

            Assert.That(identifierStatement.Token, Is.EqualTo(new Token(TokenType.Ident, text)));
            Assert.That(identifierStatement.Value, Is.EqualTo(text));
            AssertNoErrors(parser);
        }

        [TestCase("5")]
        [TestCase("1")]
        [TestCase("12")]
        [TestCase("123456789")]
        public void Simple_int_parse(string text)
        {
            var parser = new Parser(new Lexer(text));
            var program = parser.ParseProgram();
            var expressionStatement = (ExpressionStatement)program.Statements[0];
            var integerLiteral = (IntegerLiteral)expressionStatement.Expression;

            Assert.That(integerLiteral.Token, Is.EqualTo(new Token(TokenType.Int, text)));
            Assert.That(integerLiteral.Value, Is.EqualTo(Int64.Parse(text)));
            AssertNoErrors(parser);
        }

        [TestCase("-5", "5", 5L)]
        [TestCase("-1", "1", 1L)]
        [TestCase("-12", "12", 12L)]
        [TestCase("-123456789", "123456789", 123456789L)]
        public void Parse_negative_number(string text, string intText, Int64 intValue)
        {
            var integerLiteral = new IntegerLiteral { Token = new Token(TokenType.Int, intText), Value = intValue };
            var parser = new Parser(new Lexer(text));
            var program = parser.ParseProgram();
            var expressionStatement = (ExpressionStatement)program.Statements[0];
            var prefixExpr = (PrefixExpression)expressionStatement.Expression;

            Assert.That(prefixExpr.Token, Is.EqualTo(new Token(TokenType.Minus, "-")));
            Assert.That(prefixExpr.Operator, Is.EqualTo("-"));
            Assert.That(prefixExpr.Right, Is.EqualTo(integerLiteral));
            AssertNoErrors(parser);
        }

        [TestCase("5 + 5", "+", TokenType.Plus)]
        [TestCase("5 - 5", "-", TokenType.Minus)]
        [TestCase("5 / 5", "/", TokenType.Slash)]
        [TestCase("5 * 5", "*", TokenType.Asterisk)]
        [TestCase("5 > 5", ">", TokenType.Gt)]
        [TestCase("5 < 5", "<", TokenType.Lt)]
        public void Parse_binary(string text, string operatorText, TokenType tokenType)
        {
            var parser = new Parser(new Lexer(text));
            var program = parser.ParseProgram();
            var expressionStatement = (ExpressionStatement)program.Statements[0];
            Debug.WriteLine(expressionStatement);
            var infixExpr = (InfixExpression)expressionStatement.Expression;

            Assert.That(infixExpr.Token.Type, Is.EqualTo(tokenType));
            Assert.That(infixExpr.Operator, Is.EqualTo(operatorText));

            AssertNoErrors(parser);
        }

        [TestCase("-1", "-", TokenType.Minus)]
        [TestCase("!1", "!", TokenType.Bang)]
        public void Parse_unary(string text, string operatorText, TokenType tokenType)
        {
            var parser = new Parser(new Lexer(text));
            var program = parser.ParseProgram();
            var expressionStatement = (ExpressionStatement)program.Statements[0];
            var prefixExpr = (PrefixExpression)expressionStatement.Expression;

            Assert.That(prefixExpr.Token.Type, Is.EqualTo(tokenType));
            Assert.That(prefixExpr.Operator, Is.EqualTo(operatorText));

            AssertNoErrors(parser);
        }

        [TestCase("true", TokenType.True, true)]
        [TestCase("false", TokenType.False, false)]
        public void Parse_boolean(string text, TokenType tokenType, bool value)
        {
            var parser = new Parser(new Lexer(text));
            var program = parser.ParseProgram();
            var expressionStatement = (ExpressionStatement)program.Statements[0];
            var expr = (Robin.Boolean)expressionStatement.Expression;

            Assert.That(expr.Token.Type, Is.EqualTo(tokenType));
            Assert.That(expr.Value, Is.EqualTo(value));

            AssertNoErrors(parser);
        }

        private void AssertNoErrors(Parser p)
        {
            Assert.That(p.Errors, Is.EquivalentTo(new string[0]));
        }
    }
}
