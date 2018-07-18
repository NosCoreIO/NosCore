using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection.Emit;

namespace NosCore.Core.Extensions
{
    public static class TypeExtension
    {
        private static readonly ConcurrentDictionary<Type, Func<object>> Constructors =
            new ConcurrentDictionary<Type, Func<object>>();

        public static Func<TBase> GetConstructorDelegate<TBase>(this Type type)
        {
            return (Func<TBase>) GetConstructorDelegate(type, typeof(Func<TBase>));
        }

        public static Func<object> GetConstructorDelegate(this Type type)
        {
            return (Func<object>) GetConstructorDelegate(type, typeof(Func<object>));
        }

        public static Delegate GetConstructorDelegate(this Type type, Type delegateType)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (delegateType == null)
            {
                throw new ArgumentNullException(nameof(delegateType));
            }

            var genericArguments = delegateType.GetGenericArguments();
            var argTypes = genericArguments.Length > 1 ? genericArguments.Take(genericArguments.Length - 1).ToArray()
                : Type.EmptyTypes;

            var constructor = type.GetConstructor(argTypes);
            if (constructor == null)
            {
                if (argTypes.Length == 0)
                {
                    throw new InvalidProgramException(
                        string.Format("Type '{0}' doesn't have a parameterless constructor.", type.Name));
                }

                throw new InvalidProgramException(string.Format("Type '{0}' doesn't have the requested constructor.",
                    type.Name));
            }

            var dynamicMethod = new DynamicMethod("DM$_" + type.Name, type, argTypes, type);
            var ilGen = dynamicMethod.GetILGenerator();
            for (var i = 0; i < argTypes.Length; i++)
            {
                ilGen.Emit(OpCodes.Ldarg, i);
            }

            ilGen.Emit(OpCodes.Newobj, constructor);
            ilGen.Emit(OpCodes.Ret);
            return dynamicMethod.CreateDelegate(delegateType);
        }

        public static T CreateInstance<T>(this Type type)
        {
            if (Constructors.TryGetValue(type, out var constructor))
            {
                return (T) constructor();
            }

            constructor = type.GetConstructorDelegate();
            Constructors.TryAdd(type, constructor);

            return (T) constructor();
        }
    }
}