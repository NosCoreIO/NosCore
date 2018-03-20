using System;
using System.Collections.Generic;

namespace NosCore.Core
{
    public static class PacketFinder
    {
        public static IEnumerable<T> GetInstancesOfImplementingTypes<T>(Type type)
        {
            foreach (Type t in type.Assembly.GetTypes())
            {
                if (typeof(T).IsAssignableFrom(t))
                {
                    yield return (T)t.CreateInstance();
                }
            }
        }
    }
}