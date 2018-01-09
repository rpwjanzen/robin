using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robin
{
    public delegate Expression PrefixFn();
    public delegate Expression InfixFn(Expression e);

    enum Precedence { Lowest, Equal, LessGreater, Sum, Product, Prefix, Call }

    public sealed class Parser
    {
        private readonly Lexer lexer;

        private Token currentToken;
        private Token peekToken;
        public List<string> Errors = new List<string>();
        private Dictionary<TokenType, PrefixFn> prefixParseFns = new Dictionary<TokenType, PrefixFn>();
        private Dictionary<TokenType, InfixFn> infixParseFns = new Dictionary<TokenType, InfixFn>();
        private Dictionary<TokenType, Precedence> precedences = new Dictionary<TokenType, Precedence>
        {
            { TokenType.Eq, Precedence.Equal },
            { TokenType.NotEq, Precedence.Equal },
            { TokenType.Lt, Precedence.LessGreater },
            { TokenType.Gt, Precedence.LessGreater },
            { TokenType.Plus, Precedence.Sum },
            { TokenType.Minus, Precedence.Sum },
            { TokenType.Asterisk, Precedence.Product },
            { TokenType.Slash, Precedence.Product },
        };

        private Precedence PeekPrecedence()
        {
            if (precedences.TryGetValue(peekToken.Type, out Precedence value))
            {
                return value;
            }

            return Precedence.Lowest;
        }

        private Precedence CurrentPrecedence()
        {
            if (precedences.TryGetValue(currentToken.Type, out Precedence value))
            {
                return value;
            }

            return Precedence.Lowest;
        }
        public void RegisterPrefix(TokenType tokenType, PrefixFn prefixFn)
        {
            prefixParseFns.Add(tokenType, prefixFn);
        }

        public void RegisterInfix(TokenType tokenType, InfixFn infixFn)
        {
            infixParseFns.Add(tokenType, infixFn);
        }

        public Parser(Lexer lexer)
        {
            this.lexer = lexer;

            NextToken();
            NextToken();

            RegisterPrefix(TokenType.Ident, ParseIdentifier);
            RegisterPrefix(TokenType.Int, ParseInt);
            RegisterPrefix(TokenType.Bang, ParsePrefixExpression);
            RegisterPrefix(TokenType.Minus, ParsePrefixExpression);
            RegisterPrefix(TokenType.True, ParseBoolean);
            RegisterPrefix(TokenType.False, ParseBoolean);
            RegisterPrefix(TokenType.LParen, ParseGroupedExpression);

            RegisterInfix(TokenType.Plus, ParseInfixExpression);
            RegisterInfix(TokenType.Minus, ParseInfixExpression);
            RegisterInfix(TokenType.Lt, ParseInfixExpression);
            RegisterInfix(TokenType.Gt, ParseInfixExpression);
            RegisterInfix(TokenType.Asterisk, ParseInfixExpression);
            RegisterInfix(TokenType.Slash, ParseInfixExpression);
            RegisterInfix(TokenType.Eq, ParseInfixExpression);
            RegisterInfix(TokenType.NotEq, ParseInfixExpression);
        }

        private Expression ParseGroupedExpression()
        {
            // move past '('
            NextToken();

            var expr = ParseExpression(Precedence.Lowest);

            // move past ')'
            if (!ExpectPeek(TokenType.RParen))
            {
                return null;
            }

            return expr;
        }

        private Expression ParseBoolean()
        {
            return new Boolean { Token = currentToken, Value = CurrentTokenIs(TokenType.True) };
        }

        private Expression ParseInfixExpression(Expression left)
        {
            var expr = new InfixExpression
            {
                Token = currentToken,
                Operator = currentToken.Literal,
                Left = left
            };
            var precedence = CurrentPrecedence();
            // skip current token
            NextToken();
            expr.Right = ParseExpression(precedence);

            return expr;
        }

        private Expression ParsePrefixExpression()
        {
            var expr = new PrefixExpression { Token = currentToken, Operator = currentToken.Literal };
            
            // move past prefix token
            NextToken();

            expr.Right = ParseExpression(Precedence.Prefix);

            return expr;
        }

        private void NoPrefixParseFunctionFound(TokenType tokenType)
        {
            Errors.Add(string.Format("No prefix parse function for %s found.", tokenType.ToString()));
        }

        private Expression ParseInt()
        {
            var integer = new IntegerLiteral { Token = currentToken };
            if (!Int64.TryParse(currentToken.Literal, out long result))
            {
                Errors.Add(String.Format("Cannot parse %s as an integer", currentToken.Literal));
            }
            else
            {
                integer.Value = result;
            }

            return integer;
        }

        private Expression ParseIdentifier() => new Identifier { Token = currentToken, Value = currentToken.Literal };

        public void NextToken()
        {
            currentToken = peekToken;
            peekToken = lexer.NextToken();
        }

        public void PeekError(TokenType tokenType)
        {
            Errors.Add(String.Format("Expected %s, found %s.", tokenType.ToString(), peekToken.ToString()));
        }

        public MonkeyProgram ParseProgram()
        {
            var statements = new List<Statement>();
            while (currentToken.Type != TokenType.Eof)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }
                NextToken();
            }

            return new MonkeyProgram { Statements = statements.ToArray() };
        }

        private Statement ParseStatement()
        {
            switch (currentToken.Type)
            {
                case TokenType.Let:
                    return ParseLetStatement();
                case TokenType.Return:
                    return ParseReturnStatement();
                default:
                    return ParseExpressionStatement();
            }
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            var expressionStatement = new ExpressionStatement { Token = currentToken };
            expressionStatement.Expression = ParseExpression(Precedence.Lowest);

            // skip optional semicolon
            if (PeekTokenIs(TokenType.Semicolon))
            {
                NextToken();
            }

            return expressionStatement;
        }

        private Expression ParseExpression(Precedence p)
        {
            var prefix = prefixParseFns[currentToken.Type];
            if (prefix == null)
            {
                NoPrefixParseFunctionFound(currentToken.Type);
                return null;
            }

            var leftExpr = prefix();

            while(!PeekTokenIs(TokenType.Semicolon) && p < PeekPrecedence())
            {
                if (!infixParseFns.TryGetValue(peekToken.Type, out InfixFn infixFn))
                {
                    return leftExpr;
                }

                NextToken();

                leftExpr = infixFn(leftExpr);
            }

            return leftExpr;
        }

        private LetStatement ParseLetStatement()
        {
            var letStatement = new LetStatement { Token = currentToken };

            // move past identifier (e.g. 'foo')
            if (!ExpectPeek(TokenType.Ident))
            {
                return null;
            }

            letStatement.Name = new Identifier { Token = currentToken, Value = currentToken.Literal };

            // move past '='
            if (!ExpectPeek(TokenType.Assign))
            {
                return null;
            }

            // TODO: We're skipping the expressions until we
            // encounter a semicolon
            while (!CurrentTokenIs(TokenType.Semicolon))
            {
                NextToken();
            }

            return letStatement;
        }

        private bool CurrentTokenIs(TokenType tokenType) => currentToken.Type == tokenType;

        private bool PeekTokenIs(TokenType tokenType) => peekToken.Type == tokenType;

        private bool ExpectPeek(TokenType tokenType)
        {
            if (PeekTokenIs(tokenType))
            {
                NextToken();
                return true;
            }

            PeekError(tokenType);
            return false;
        }

        private ReturnStatement ParseReturnStatement()
        {
            var returnStatement = new ReturnStatement { Token = currentToken };
            NextToken();

            // TODO: We're skipping the expressions until we
            // encounter a semicolon
            while (!CurrentTokenIs(TokenType.Semicolon))
            {
                NextToken();
            }

            return returnStatement;
        }
    }
}
