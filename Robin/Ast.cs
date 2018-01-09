using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robin
{
    public interface Node { }
    public interface Statement : Node { }
    public interface Expression : Node { }

    public sealed class IfExpression : Expression
    {
        public Token Token;
        public Expression Condition;
        public BlockStatement Consequent;
        public BlockStatement Alternative;

        public override string ToString()
        {
            if (Alternative != null)
            {
                return String.Format("if (%s) then %s else %s", Condition.ToString(), Consequent.ToString(), Alternative.ToString());
            }

            return String.Format("if (%s) then %s", Condition.ToString(), Consequent.ToString());
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

    public sealed class Boolean : Expression
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

    public sealed class Identifier : Expression
    {
        public Token Token;
        public string Value;
    }

    public sealed class LetStatement : Statement
    {
        public Token Token;
        public Expression Name;
        public string Value;
    }

    public sealed class ReturnStatement : Statement
    {
        public Token Token;
        public Expression ReturnValue;

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

    public sealed class PrefixExpression : Expression
    {
        public Token Token;
        public string Operator;
        public Expression Right;

        public override string ToString()
        {
            return string.Format("(%s%s)", Operator, Right.ToString());
        }
    }

    public sealed class InfixExpression : Expression
    {
        public Token Token;
        public Expression Left;
        public string Operator;
        public Expression Right;

        public override string ToString()
        {
            return string.Format("(%s %s %s)", Left.ToString(), Operator, Right.ToString());
        }
    }

    public sealed class ExpressionStatement : Statement
    {
        // first token of the expression
        public Token Token;

        public Expression Expression;
    }

    public sealed class IntegerLiteral : Expression
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
        public Statement[] Statements;

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
