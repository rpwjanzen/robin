using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robin
{
    public interface INode { }
    public interface IStatement : INode { }
    public interface IExpression : INode { }

    public sealed class IfExpression : IExpression
    {
        public Token Token;
        public IExpression Condition;
        public BlockStatement Consequent;
        public BlockStatement Alternative;

        public override string ToString()
        {
            if (Alternative != null)
            {
                return $"if ({Condition}) then {Consequent} else {Alternative}";
            }

            return $"if ({Condition} then {Consequent}";
        }

        public override bool Equals(object obj)
        {
            var other = obj as IfExpression;
            if (other == null)
            {
                return false;
            }

            if (Alternative == null && other.Alternative != null)
            {
                return false;
            }
            if (Alternative != null && other.Alternative == null)
            {
                return false;
            }
            if (Alternative == null && other.Alternative == null)
            {
                return Token.Equals(other.Token) && Condition.Equals(other.Condition) && Consequent.Equals(other.Consequent);
            }

            return Token.Equals(other.Token) && Condition.Equals(other.Condition) && Consequent.Equals(other.Consequent) && Alternative.Equals(other.Alternative);
        }

        public override int GetHashCode()
        {
            return Token.GetHashCode() << 24 ^ Condition.GetHashCode() << 16 ^ Consequent.GetHashCode() << 8 ^ (Alternative == null ? 0 : Alternative.GetHashCode());
        }
    }

        public sealed class BlockStatement
    {

    }

    public sealed class Boolean : IExpression
    {
        public Token Token;
        public bool Value;

        public override bool Equals(object obj)
        {
            var other = obj as Boolean;
            if (other == null)
            {
                return false;
            }

            return Token.Equals(other.Token) && Value.Equals(other.Value);
        }

        public override int GetHashCode()
        {
            return Token.GetHashCode() ^ Value.GetHashCode();
        }
    }

    public sealed class Identifier : IExpression
    {
        public Token Token;
        public string Value;
    }

    public sealed class LetStatement : IStatement
    {
        public Token Token;
        public IExpression Name;
        public string Value;
    }

    public sealed class ReturnStatement : IStatement
    {
        public Token Token;
        public IExpression ReturnValue;

        public override bool Equals(object obj)
        {
            var other = obj as ReturnStatement;
            if (other == null)
            {
                return false;
            }

            return Token.Equals(other.Token) && ReturnValue.Equals(other.ReturnValue);
        }

        public override int GetHashCode()
        {
            return Token.GetHashCode() ^ ReturnValue.GetHashCode();
        }
    }

    public sealed class PrefixExpression : IExpression
    {
        public Token Token;
        public string Operator;
        public IExpression Right;

        public override string ToString()
        {
            return $"({Operator}{Right})";
        }
    }

    public sealed class InfixExpression : IExpression
    {
        public Token Token;
        public IExpression Left;
        public string Operator;
        public IExpression Right;

        public override string ToString()
        {
            return $"({Left} {Operator} {Right})";
        }
    }

    public sealed class ExpressionStatement : IStatement
    {
        // first token of the expression
        public Token Token;

        public IExpression Expression;
    }

    public sealed class IntegerLiteral : IExpression
    {
        public Token Token;
        public Int64 Value;

        public override bool Equals(object obj)
        {
            var other = obj as IntegerLiteral;
            if (other == null)
            {
                return false;
            }
            return other.Value.Equals(Value) && other.Token.Equals(Token);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ Token.GetHashCode();
        }
    }

    public sealed class MonkeyProgram
    {
        public IStatement[] Statements;

        public override string ToString()
        {
            if (Statements.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return Statements[0].ToString();
            }
        }
    }
}
