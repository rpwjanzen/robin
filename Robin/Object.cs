﻿using System;

namespace Robin.Obj
{
    enum ObjectType { Int, Boolean, Null, ReturnValue, Error, Function, String, Builtin, Array, Hash, Quote, Macro }
    interface IObject
    {
        ObjectType Type();
        string Inspect();
    }

    class Macro : IObject
    {
        public Ast.Identifier[] Parameters;
        public Ast.BlockStatement Body;
        public Environment Env;
        public string Inspect()
        {
            var args = new System.Collections.Generic.List<string>();
            foreach (var p in Parameters)
            {
                args.Add(p.ToString());
            }

            return $"macro({System.String.Join(", ", args)})" + "{" + System.Environment.NewLine
                + Body.ToString() + System.Environment.NewLine + "}";
        }

        public ObjectType Type() => ObjectType.Macro;
    }

    class Quote : IObject
    {
        public Ast.INode Node;
        public ObjectType Type() => ObjectType.Quote;
        public string Inspect() => $"QUOTE({Node})";
    }

    class Array : IObject
    {
        public IObject[] Elements;
        public ObjectType Type() => ObjectType.Array;
        public string Inspect()
        {
            var rs = new System.Collections.Generic.List<string>();
            foreach (var x in Elements)
            {
                rs.Add(x.Inspect());
            }
            return $"[{string.Join(", ", rs)}]";
        }
    }

    class Integer : IObject, Hashable
    {
        public Int64 Value;

        public string Inspect()
        {
            return $"{Value}";
        }

        public ObjectType Type()
        {
            return ObjectType.Int;
        }

        public override bool Equals(object obj)
        {
            return (obj is Integer i) && Value.Equals(i.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public HashKey HashKey()
        {
            return new Obj.HashKey { Type = ObjectType.Int, Value = (UInt64)Value };
        }
    }

    class Boolean : IObject, Hashable
    {
        public readonly bool Value;

        private Boolean(bool value) { Value = value; }

        public static readonly Boolean True = new Boolean(true);
        public static readonly Boolean False = new Boolean(false);

        public string Inspect()
        {
            return $"{Value}";
        }

        public ObjectType Type()
        {
            return ObjectType.Boolean;
        }

        public override bool Equals(object obj)
        {
            return (obj is Boolean b) && Value.Equals(b.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public HashKey HashKey()
        {
            return new HashKey
            {
                Type = ObjectType.Boolean,
                Value = Value == true ? 1UL : 0UL
            };
        }
    }

    class Null : IObject
    {
        private Null() { }

        public static readonly Null Instance = new Null();

        public string Inspect()
        {
            return "null";
        }

        public ObjectType Type()
        {
            return ObjectType.Null;
        }

        public override bool Equals(object obj)
        {
            return (obj is Null n);
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    class ReturnValue : IObject
    {
        public IObject Value;

        public string Inspect()
        {
            return Value.Inspect();
        }

        public ObjectType Type()
        {
            return ObjectType.ReturnValue;
        }
    }

    class Error : IObject
    {
        public string Message;

        public string Inspect()
        {
            return "Error: " + Message;
        }

        public ObjectType Type()
        {
            return ObjectType.Error;
        }
    }

    class Function : IObject
    {
        public Ast.Identifier[] Parameters;
        public Ast.BlockStatement Body;
        public Environment Env;

        public string Inspect()
        {
            return $"fn({System.String.Join(", ", (System.Collections.Generic.IEnumerable<Object>)Parameters)}) {{{System.Environment.NewLine}{Body}";
        }

        public ObjectType Type()
        {
            return ObjectType.Function;
        }
    }

    class String : IObject, Hashable
    {
        public string Value;

        public HashKey HashKey()
        {
            return new HashKey
            {
                Type = ObjectType.String,
                Value = (UInt64)Value.GetHashCode()
            };
        }

        public string Inspect()
        {
            return Value;
        }

        public ObjectType Type()
        {
            return ObjectType.String;
        }
    }

    delegate IObject BuiltinFunction(params IObject[] args);

    class Builtin : IObject
    {
        public BuiltinFunction Fn;

        public string Inspect()
        {
            return "builtin function";
        }

        public ObjectType Type()
        {
            return ObjectType.Builtin;
        }
    }

    class HashKey
    {
        public ObjectType Type;
        public UInt64 Value;

        public override bool Equals(object obj)
        {
            var other = obj as HashKey;
            if (other == null)
            {
                return false;
            }

            return Type.Equals(other.Type) && Value.Equals(other.Value);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ Value.GetHashCode();
        }
    }

    sealed class HashPair
    {
        public IObject Key;
        public IObject Value;
    }

    class Hash : IObject
    {
        public System.Collections.Generic.Dictionary<HashKey, HashPair> Pairs;
        public ObjectType Type() => ObjectType.Hash;

        public string Inspect()
        {
            var pairs = new System.Collections.Generic.List<string>();
            foreach(var pair in Pairs)
            {
                pairs.Add($"{pair.Value.Key.Inspect()}: {pair.Value.Value.Inspect()}");
            }

            return "{" + System.String.Join(", ", pairs) + "}";
        }
    }

    interface Hashable
    {
        HashKey HashKey();
    }
}
