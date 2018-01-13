using Robin.Obj;
using System;
using System.Collections.Generic;

namespace Robin
{
    class Environment
    {
        private Dictionary<string, IObject> env = new Dictionary<string, IObject>();

        public bool TryGet(string name, out IObject obj)
        {
            return env.TryGetValue(name, out obj);
        }

        public T Set<T>(string name, T value) where T : IObject
        {
            env[name] = value;
            return value;
        }
    }
}
