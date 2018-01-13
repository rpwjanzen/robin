using System;

namespace Robin.Obj
{
    enum ObjectType { Int, Boolean, Null, ReturnValue, Error }
    interface IObject
    {
        ObjectType Type();
        string Inspect();
    }

    class Integer : IObject
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
    }

    class Boolean : IObject
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
}
