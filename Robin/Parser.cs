namespace Robin.Parsing
{
    using Robin.Ast;
    using Robin.Lexing;
    using System;
    using System.Collections.Generic;
    using Boolean = Ast.Boolean;

    public delegate IExpression PrefixFn();
    public delegate IExpression InfixFn(IExpression e);

    enum Precedence { Lowest, Equal, LessGreater, Sum, Product, Prefix, Call, Index }

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
            { TokenType.LParen, Precedence.Call },
            { TokenType.LBracket, Precedence.Index }
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
            RegisterPrefix(TokenType.If, ParseIfExpression);
            RegisterPrefix(TokenType.Function, ParseFunctionLiteral);
            RegisterPrefix(TokenType.String, ParseStringLiteral);
            RegisterPrefix(TokenType.LBracket, ParseArrayLiteral);
            RegisterPrefix(TokenType.LBrace, ParseHashLiteral);

            RegisterInfix(TokenType.Plus, ParseInfixExpression);
            RegisterInfix(TokenType.Minus, ParseInfixExpression);
            RegisterInfix(TokenType.Lt, ParseInfixExpression);
            RegisterInfix(TokenType.Gt, ParseInfixExpression);
            RegisterInfix(TokenType.Asterisk, ParseInfixExpression);
            RegisterInfix(TokenType.Slash, ParseInfixExpression);
            RegisterInfix(TokenType.Eq, ParseInfixExpression);
            RegisterInfix(TokenType.NotEq, ParseInfixExpression);
            RegisterInfix(TokenType.LParen, ParseCallExpression);
            RegisterInfix(TokenType.LBracket, ParseIndexExpression);
        }

        private IExpression ParseHashLiteral()
        {
            var hash = new HashLiteral { Token = currentToken };
            hash.Pairs = new Dictionary<IExpression, IExpression>();

            while (!PeekTokenIs(TokenType.RBrace))
            {
                NextToken();
                var key = ParseExpression(Precedence.Lowest);

                if (!ExpectPeek(TokenType.Colon))
                {
                    return null;
                }
                NextToken();

                var value = ParseExpression(Precedence.Lowest);
                hash.Pairs.Add(key, value);

                if (!PeekTokenIs(TokenType.RBrace) && !ExpectPeek(TokenType.Comma))
                {
                    return null;
                }
            }

            if (!ExpectPeek(TokenType.RBrace))
            {
                return null;
            }

            return hash;
        }

        private IExpression ParseIndexExpression(IExpression left)
        {
            var exp = new IndexExpression { Token = currentToken, Left = left };
            NextToken();
            exp.Index = ParseExpression(Precedence.Lowest);
            if (!ExpectPeek(TokenType.RBracket))
            {
                return null;
            }

            return exp;
        }

        private IExpression ParseArrayLiteral()
        {
            var arr = new ArrayLiteral { Token = this.currentToken };
            arr.Elements = ParseExpressionList(TokenType.RBracket);
            return arr;
        }

        private IExpression[] ParseExpressionList(TokenType end)
        {
            var list = new List<IExpression>();
            if (PeekTokenIs(end))
            {
                // skip end token
                NextToken();
                return list.ToArray();
            }

            // skip start token?
            NextToken();

            // parse first item in array
            list.Add(ParseExpression(Precedence.Lowest));

            // parse rest of item in array
            while(PeekTokenIs(TokenType.Comma))
            {
                // skip comma
                NextToken();
                NextToken();

                list.Add(ParseExpression(Precedence.Lowest));
            }

            if (!ExpectPeek(end))
            {
                Console.WriteLine("null");
                return null;
            }

            return list.ToArray();
        }

        private IExpression ParseStringLiteral()
        {
            return new StringLiteral { Token = currentToken, Value = currentToken.Literal };
        }

        private IExpression ParseCallExpression(IExpression function)
        {
            var exp = new CallExpression { Token = currentToken, Function = function };
            exp.Arguments = ParseExpressionList(TokenType.RParen);
            return exp;
        }

        private IExpression[] ParseCallArguments()
        {
            var args = new List<IExpression>();

            if (PeekTokenIs(TokenType.RParen))
            {
                // ')'
                NextToken();
                return args.ToArray();
            }
            // ')'
            NextToken();

            args.Add(ParseExpression(Precedence.Lowest));

            while(PeekTokenIs(TokenType.Comma))
            {
                // ','
                NextToken();
                // ???
                NextToken();

                args.Add(ParseExpression(Precedence.Lowest));
            }

            if (!ExpectPeek(TokenType.RParen))
            {
                return null;
            }

            return args.ToArray();
        }

        private IExpression ParseFunctionLiteral()
        {
            var lit = new FunctionLiteral() { Token = currentToken };
            if (!ExpectPeek(TokenType.LParen))
            {
                return null;
            }

            lit.Parameters = ParseFunctionParameters();

            if (!ExpectPeek(TokenType.LBrace))
            {
                return null;
            }

            lit.Body = ParseBlockStatement();

            return lit;
        }

        private Identifier[] ParseFunctionParameters()
        {
            // ')'
            if (PeekTokenIs(TokenType.RParen))
            {
                // ')'
                NextToken();

                return new Identifier[0]; 
            }

            // ')'
            NextToken();

            var identifiers = new List<Identifier>();
            var ident = new Identifier { Token = currentToken, Value = currentToken.Literal };
            identifiers.Add(ident);

            while(PeekTokenIs(TokenType.Comma))
            {
                // ','
                NextToken();

                // ???
                NextToken();

                ident = new Identifier { Token = currentToken, Value = currentToken.Literal };
                identifiers.Add(ident);
            }

            // ')'
            if (!ExpectPeek(TokenType.RParen))
            {
                return null;
            }

            return identifiers.ToArray();
        }

        private IExpression ParseGroupedExpression()
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

        private IExpression ParseBoolean()
        {
            return new Boolean { Token = currentToken, Value = CurrentTokenIs(TokenType.True) };
        }

        private IExpression ParseInfixExpression(IExpression left)
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

        private IExpression ParsePrefixExpression()
        {
            var expr = new PrefixExpression { Token = currentToken, Operator = currentToken.Literal };
            
            // move past prefix token
            NextToken();

            expr.Right = ParseExpression(Precedence.Prefix);

            return expr;
        }

        private void NoPrefixParseFunctionFound(TokenType tokenType)
        {
            Errors.Add($"No prefix parse function for {tokenType} found.");
        }

        private IExpression ParseInt()
        {
            var integer = new IntegerLiteral { Token = currentToken };
            if (!Int64.TryParse(currentToken.Literal, out long result))
            {
                Errors.Add($"Cannot parse {currentToken.Literal} as an integer");
            }
            else
            {
                integer.Value = result;
            }

            return integer;
        }

        private IExpression ParseIdentifier() => new Identifier { Token = currentToken, Value = currentToken.Literal };

        public void NextToken()
        {
            currentToken = peekToken;
            peekToken = lexer.NextToken();
        }

        public void PeekError(TokenType tokenType)
        {
            Errors.Add($"Expected next token to be {tokenType}, found {peekToken} instead.");
        }

        public Program ParseProgram()
        {
            var statements = new List<IStatement>();
            while (currentToken.Type != TokenType.Eof)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }
                NextToken();
            }

            return new Program { Statements = statements.ToArray() };
        }

        private IStatement ParseStatement()
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

        private IExpression ParseExpression(Precedence p)
        {
            if (!prefixParseFns.TryGetValue(currentToken.Type, out PrefixFn prefix)) 
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

            // ???
            NextToken();

            letStatement.Value = ParseExpression(Precedence.Lowest);

            // optional semicolon
            if (PeekTokenIs(TokenType.Semicolon))
            {
                // ';'
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
            // 'return'
            NextToken();

            returnStatement.ReturnValue = ParseExpression(Precedence.Lowest);

            // optional semicolon
            if (PeekTokenIs(TokenType.Semicolon))
            {
                // ';'
                NextToken();
            }

            return returnStatement;
        }

        private IExpression ParseIfExpression()
        {
            // 'if'
            var expression = new IfExpression() { Token = currentToken };

            // '('
            if (!ExpectPeek(TokenType.LParen))
            {
                return null;
            }
            // ???
            NextToken();

            expression.Condition = ParseExpression(Precedence.Lowest);

            // ')'
            if (!ExpectPeek(TokenType.RParen))
            {
                return null;
            }
            
            // '{'
            if (!ExpectPeek(TokenType.LBrace))
            {
                return null;
            }

            expression.Consequent = ParseBlockStatement();

            // optional else
            if (PeekTokenIs(TokenType.Else))
            {
                // 'else'
                NextToken();

                // '{'
                if (!ExpectPeek(TokenType.LBrace))
                {
                    return null;
                }

                expression.Alternative = ParseBlockStatement();
            }

            return expression;
        }

        private BlockStatement ParseBlockStatement()
        {
            var block = new BlockStatement { Token = currentToken };
            var statements = new List<IStatement>();
            // skip '{'
            NextToken();

            while (!CurrentTokenIs(TokenType.RBrace) && !CurrentTokenIs(TokenType.Eof))
            {
                var stmt = ParseStatement();
                if (stmt != null)
                {
                    statements.Add(stmt);
                }
                // ???
                NextToken();
            }

            block.Statements = statements.ToArray();

            return block;
        }
    }
}
