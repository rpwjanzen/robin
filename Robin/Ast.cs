namespace Robin.Ast
{
    using System;
    using System.Collections.Generic;

    public interface INode { }
    public interface IStatement : INode
    {
        string TokenLiteral();
    }
    public interface IExpression : INode
    {
        string TokenLiteral();
    }

    public sealed class IfExpression : IExpression
    {
        public Token Token;
        public IExpression Condition;
        public BlockStatement Consequent;
        public BlockStatement Alternative;
        public string TokenLiteral() => Token.Literal;

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

    public sealed class BlockStatement : IStatement
    {
        public Token Token;
        public IStatement[] Statements;
        public string TokenLiteral() => Token.Literal;

        public override string ToString()
        {
            return String.Join("", (IEnumerable<IStatement>)Statements);
        }
    }

    public sealed class Boolean : IExpression
    {
        public Token Token;
        public bool Value;
        public string TokenLiteral() => Token.Literal;

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

        public override string ToString()
        {
            return $"{Value}";
        }
    }

    public sealed class Identifier : IExpression
    {
        public Token Token;
        public string Value;
        public string TokenLiteral() => Token.Literal;

        public override string ToString()
        {
            return $"{Value}";
        }
    }

    public sealed class LetStatement : IStatement
    {
        public Token Token;
        public Identifier Name;
        public IExpression Value;
        public string TokenLiteral() => Token.Literal;

        public override string ToString()
        {
            return $"let {Name} = {Value}";
        }
    }

    public sealed class ReturnStatement : IStatement
    {
        public Token Token;
        public IExpression ReturnValue;
        public string TokenLiteral() => Token.Literal;

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

        public override string ToString()
        {
            return $"return {ReturnValue}";
        }
    }

    public sealed class PrefixExpression : IExpression
    {
        public Token Token;
        public string Operator;
        public IExpression Right;
        public string TokenLiteral() => Token.Literal;

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
        public string TokenLiteral() => Token.Literal;

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
        public string TokenLiteral() => Token.Literal;

        public override string ToString()
        {
            return $"{Expression}";
        }
    }

    public sealed class IntegerLiteral : IExpression
    {
        public Token Token;
        public Int64 Value;
        public string TokenLiteral() => Token.Literal;

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

        public override string ToString()
        {
            return $"{Value}";
        }
    }

    public sealed class Program : INode
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

    public sealed class FunctionLiteral : IExpression
    {
        public Token Token;
        public Identifier[] Parameters;
        public BlockStatement Body;
        public string TokenLiteral() => Token.Literal;

        public override string ToString()
        {
            return $"{Token.Literal} ({String.Join(", ", (IEnumerable<Identifier>)Parameters)}) {Body}";
        }
    }

    public sealed class CallExpression : IExpression
    {
        public Token Token;
        public IExpression Function;
        public IExpression[] Arguments;

        public string TokenLiteral() => Token.Literal;
        public override string ToString()
        {
            return $"{Function} ({String.Join(", ", (IEnumerable<IExpression>)Arguments)})";
        }
    }

    public sealed class StringLiteral : IExpression
    {
        public Token Token;
        public string Value;

        public string TokenLiteral() { return Token.Literal; }
        public string String() { return Token.Literal; }
    }

    public sealed class ArrayLiteral : IExpression
    {
        public Token Token;
        public IExpression[] Elements;

        public string TokenLiteral() => Token.Literal;
        public override string ToString()
        {
            return $"[{String.Join(", ", (IEnumerable<IExpression>)Elements)}]";
        }
    }

    public sealed class IndexExpression : IExpression
    {
        public Token Token;
        public IExpression Left;
        public IExpression Index;

        public string TokenLiteral() => Token.Literal;
        public override string ToString() => $"({Left}[{Index}])";
    }

    public sealed class HashLiteral : IExpression
    {
        public Token Token;
        public Dictionary<IExpression, IExpression> Pairs;

        public string TokenLiteral() => Token.Literal;
        public override string ToString()
        {
            var text = new List<string>();
            foreach (var kvp in Pairs)
            {
                text.Add($"{kvp.Key}:{kvp.Value}");
            }

            return string.Join(", ", text);
        }
    }

    delegate INode ModifierFunc(INode node);

    sealed class Modifier
    {
        public static INode Modify(INode node, ModifierFunc modifier)
        {
            if (node is Program p)
            {
                for (var i = 0; i < p.Statements.Length; i++)
                {
                    p.Statements[i] = (IStatement)Modify(p.Statements[i], modifier);
                }
            }
            else if (node is ExpressionStatement es)
            {
                es.Expression = (IExpression)Modify(es.Expression, modifier);
            }
            else if (node is InfixExpression ie)
            {
                ie.Left = (IExpression)Modify(ie.Left, modifier);
                ie.Right = (IExpression)Modify(ie.Right, modifier);
            }
            else if (node is PrefixExpression pe)
            {
                pe.Right = (IExpression)Modify(pe.Right, modifier);
            }
            else if (node is IndexExpression idxe)
            {
                idxe.Left = (IExpression)Modify(idxe.Left, modifier);
                idxe.Index = (IExpression)Modify(idxe.Index, modifier);
            }
            else if (node is IfExpression ife)
            {
                ife.Condition = (IExpression)Modify(ife.Condition, modifier);
                ife.Consequent = (BlockStatement)Modify(ife.Consequent, modifier);
                if (ife.Alternative != null)
                {
                    ife.Alternative = (BlockStatement)Modify(ife.Alternative, modifier);
                }
            }
            else if (node is BlockStatement bs)
            {
                for (var i = 0; i < bs.Statements.Length; i++)
                {
                    bs.Statements[i] = (IStatement)Modify(bs.Statements[i], modifier);
                }
            }
            else if (node is ReturnStatement rs)
            {
                rs.ReturnValue = (IExpression)Modify(rs.ReturnValue, modifier);
            }
            else if (node is LetStatement ls)
            {
                ls.Value = (IExpression)Modify(ls.Value, modifier);
            }
            else if (node is FunctionLiteral fl)
            {
                for (var i = 0; i < fl.Parameters.Length; i++)
                {
                    fl.Parameters[i] = (Identifier)Modify(fl.Parameters[i], modifier);
                }
                fl.Body = (BlockStatement)Modify(fl.Body, modifier);
            }
            else if (node is ArrayLiteral al)
            {
                for (var i = 0; i < al.Elements.Length; i++)
                {
                    al.Elements[i] = (IExpression)Modify(al.Elements[i], modifier);
                }
            }
            else if (node is HashLiteral hl)
            {
                var newPairs = new Dictionary<IExpression, IExpression>();
                foreach (var kvp in hl.Pairs)
                {
                    var newKey = (IExpression)Modify(kvp.Key, modifier);
                    var newVal = (IExpression)Modify(kvp.Value, modifier);
                    newPairs[newKey] = newVal;
                }
                hl.Pairs = newPairs;
            }

            return modifier(node);
        }
    }

    class MacroLiteral : IExpression
    {
        public Token Token;
        public Identifier[] Parameters;
        public BlockStatement Body;

        public string TokenLiteral() => Token.Literal;
        public override string ToString()
        {
            var args = new List<string>();
            foreach (var p in Parameters)
            {
                args.Add(p.ToString());
            }

            return $"{TokenLiteral()}({String.Join(", ", args)}){Body}";
        }
    }
}
