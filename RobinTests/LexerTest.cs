using System;
using Robin;
using NUnit.Framework;

namespace RobinTests
{
    [TestFixture]
    public class LexerTest
    {
        [TestCase]
        public void let_expression_is_let_expression()
        {
            var text = "let a = 5;";
            var lexer = new Lexer(text);
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Let));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Ident));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Assign));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Int));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Semicolon));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Eof));
        }

        [TestCase]
        public void no_text_is_EOF()
        {
            Assert.That(new Lexer(String.Empty).NextToken().Type, Is.EqualTo( TokenType.Eof));
        }

        [TestCase]
        public void at_EOF_returns_EOF_multiple_times()
        {
            var lexer = new Lexer(String.Empty);

            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Eof));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Eof));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Eof));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Eof));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Eof));
            Assert.That(lexer.NextToken().Type, Is.EqualTo(TokenType.Eof));
        }
    }
}
