using Robin.Obj;
using System;
using System.Collections.Generic;

namespace Robin
{
    class Environment
    {
        private Dictionary<string, IObject> env = new Dictionary<string, IObject>();
        private Environment outer;

        public Environment() { }
        public Environment(Environment e)
        {
            outer = e;
        }

        public bool TryGet(string name, out IObject obj)
        {
            if (outer == null)
            {
                return env.TryGetValue(name, out obj);
            }
            else
            {
                return env.TryGetValue(name, out obj) || outer.TryGet(name, out obj); ;
            }
        }

        public T Set<T>(string name, T value) where T : IObject
        {
            env[name] = value;
            return value;
        }
    }
}
