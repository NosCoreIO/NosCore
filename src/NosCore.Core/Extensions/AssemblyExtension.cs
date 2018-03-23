using System;
using System.Collections.Generic;
using System.Reflection;

namespace NosCore.Core
{
    public static class AssemblyExtension
    {
        public static IEnumerable<T> GetInstancesOfImplementingTypes<T>(this Assembly assembly)
        {
            foreach (Type t in assembly.GetTypes())
            {
                if (typeof(T).IsAssignableFrom(t))
                {
                    yield return (T)t.CreateInstance();
                }
            }
        }
    }
}