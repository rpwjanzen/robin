using System.Collections.Generic;

namespace Robin
{
    public enum TokenType
    {
        Illegal,
        Eof,
        Ident,
        Int,
        Assign,
        Plus,
        Minus,
        Comma,
        Semicolon,
        LParen,
        RParen,
        LBrace,
        RBrace,
        Function,
        Let,
        Bang,
        Asterisk,
        Slash,
        Lt,
        Gt,
        True,
        False,
        If,
        Else,
        Return,
        Eq,
        NotEq,
    };

    public struct Token
    {
        public readonly TokenType Type;
        public readonly string Literal;

        public Token(TokenType type, string literal)
        {
            Type = type;
            Literal = literal;
        }

        public override bool Equals(object obj)
        {
            if (obj is Token)
            {
                var other = (Token)obj;
                return Type.Equals(other.Type) && Literal.Equals(other.Literal);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ Literal.GetHashCode();
        }

        public static Token Create(TokenType type, char ch)
        {
            return new Token(type, ch.ToString());
        }

        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            { "fn",  TokenType.Function },
            {  "let", TokenType.Let },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "return", TokenType.Return },
        };

        public static TokenType LookupIdent(string ident)
        {
            if (Keywords.TryGetValue(ident, out TokenType value))
            {
                return value;
            }

            return TokenType.Ident;
        }

        public override string ToString()
        {
            return "{Type: " + Type.ToString() + " , Literal: " + Literal + "}";
        }
    }
}
