namespace Robin.Lexing
{
    using System;

    public class Lexer
    {
        public readonly string Input;
        private int position;
        private int readPosition;
        private char ch;

        public Lexer(string input)
        {
            Input = input;
            ReadChar();
        }

        private void ReadChar()
        {
            if (readPosition >= Input.Length)
            {
                ch = '\0';
            }
            else
            {
                ch = Input[readPosition];
            }
            position = readPosition;
            readPosition += 1;
        }

        private char PeekChar()
        {
            if (readPosition >= Input.Length)
            {
                return '\0';
            }
            else
            {
                return Input[readPosition];
            }
        }

        public Token NextToken()
        {
            SkipWhitespace();

            Token token;
            switch (ch)
            {
                case '=':
                    if (PeekChar() == '=')
                    {
                        token = new Token(TokenType.Eq, "==");
                        ReadChar();
                    }
                    else
                    {
                        token = Token.Create(TokenType.Assign, ch);
                    }
                    break;
                case ';':
                    token = Token.Create(TokenType.Semicolon, ch);
                    break;
                case '(':
                    token = Token.Create(TokenType.LParen, ch);
                    break;
                case ')':
                    token = Token.Create(TokenType.RParen, ch);
                    break;
                case ',':
                    token = Token.Create(TokenType.Comma, ch);
                    break;
                case '+':
                    token = Token.Create(TokenType.Plus, ch);
                    break;
                case '-':
                    token = Token.Create(TokenType.Minus, ch);
                    break;
                case '{':
                    token = Token.Create(TokenType.LBrace, ch);
                    break;
                case '}':
                    token = Token.Create(TokenType.RBrace, ch);
                    break;
                case '!':
                    if (PeekChar() == '=')
                    {
                        token = new Token(TokenType.NotEq, "!=");
                        ReadChar();
                    }
                    else
                    {
                        token = Token.Create(TokenType.Bang, ch);
                    }
                    break;
                case '*':
                    token = Token.Create(TokenType.Asterisk, ch);
                    break;
                case '/':
                    token = Token.Create(TokenType.Slash, ch);
                    break;
                case '<':
                    token = Token.Create(TokenType.Lt, ch);
                    break;
                case '>':
                    token = Token.Create(TokenType.Gt, ch);
                    break;
                case '\0':
                    token = Token.Create(TokenType.Eof, '\0');
                    break;
                default:
                    if (Char.IsLetter(ch))
                    {
                        var identifier = ReadIdentifier();
                        var type = Token.LookupIdent(identifier);
                        return new Token(type, identifier);
                    }
                    else if (Char.IsDigit(ch))
                    {
                        var literal = ReadNumber();
                        var type = TokenType.Int;
                        return new Token(type, literal);
                    }
                    else
                    {
                        token = Token.Create(TokenType.Illegal, ch);
                    }
                    break;
            }

            ReadChar();
            return token;
        }

        private string ReadIdentifier()
        {
            var start = position;
            while (Char.IsLetter(ch))
            {
                ReadChar();
            }
            return Input.Substring(start, position - start);
        }

        private string ReadNumber()
        {
            var start = position;
            while (Char.IsDigit(ch))
            {
                ReadChar();
            }
            return Input.Substring(start, position - start);
        }

        private void SkipWhitespace()
        {
            while (Char.IsWhiteSpace(ch))
            {
                ReadChar();
            }
        }
    }
}
