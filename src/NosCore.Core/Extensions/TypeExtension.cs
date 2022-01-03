//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection.Emit;

namespace NosCore.Core.Extensions
{
    public static class TypeExtension
    {
        private static readonly ConcurrentDictionary<Type, Func<object>> Constructors = new();

        public static Func<TBase> GetConstructorDelegate<TBase>(this Type type)
        {
            return (Func<TBase>)GetConstructorDelegate(type, typeof(Func<TBase>));
        }

        public static Func<object> GetConstructorDelegate(this Type type)
        {
            return (Func<object>)GetConstructorDelegate(type, typeof(Func<object>));
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
                        $"Type '{type.Name}' doesn't have a parameterless constructor.");
                }

                throw new InvalidProgramException($"Type '{type.Name}' doesn't have the requested constructor.");
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
                return (T)constructor();
            }

            constructor = type.GetConstructorDelegate();
            Constructors.TryAdd(type, constructor);

            return (T)constructor();
        }
    }
}