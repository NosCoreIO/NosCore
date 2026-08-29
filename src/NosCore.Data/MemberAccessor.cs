//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;

namespace NosCore.Data
{
    // Get and set a member by name without paying reflection's per-call cost. The generated
    // DTOs mark their navigation properties internal, and the parsers write six of them by
    // name, so the delegates are emitted with skipVisibility rather than compiled from an
    // expression tree, which cannot reach a non-public member.
    public sealed class MemberAccessor
    {
        private static readonly ConcurrentDictionary<Type, MemberAccessor> Accessors = new();

        private readonly Dictionary<string, Func<object, object?>> _getters;
        private readonly Dictionary<string, Action<object, object?>> _setters;

        private MemberAccessor(Type type)
        {
            _getters = new Dictionary<string, Func<object, object?>>(StringComparer.Ordinal);
            _setters = new Dictionary<string, Action<object, object?>>(StringComparer.Ordinal);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var property in type.GetProperties(flags))
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (property.GetGetMethod(true) is { } getMethod)
                {
                    _getters[property.Name] = EmitGetter(type, property.PropertyType, getMethod, null);
                }

                if (property.GetSetMethod(true) is { } setMethod)
                {
                    _setters[property.Name] = EmitSetter(type, property.PropertyType, setMethod, null);
                }
            }

            foreach (var field in type.GetFields(flags))
            {
                _getters[field.Name] = EmitGetter(type, field.FieldType, null, field);
                if (!field.IsInitOnly)
                {
                    _setters[field.Name] = EmitSetter(type, field.FieldType, null, field);
                }
            }
        }

        // Both halves stay small enough to inline: building the message is a call the JIT can
        // leave out of line, and inlining is the difference between this and plain reflection.
        public object? this[object target, string name]
        {
            get
            {
                if (!_getters.TryGetValue(name, out var getter))
                {
                    ThrowUnknown(target, name, "readable");
                }

                return getter!(target);
            }
            set
            {
                if (!_setters.TryGetValue(name, out var setter))
                {
                    ThrowUnknown(target, name, "writable");
                }

                setter!(target, value);
            }
        }

        public static MemberAccessor For(Type type) => Accessors.GetOrAdd(type, t => new MemberAccessor(t));

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowUnknown(object target, string name, string kind) =>
            throw new ArgumentOutOfRangeException(nameof(name), name,
                $"{target.GetType().Name} has no {kind} member named '{name}'.");

        private static Func<object, object?> EmitGetter(Type owner, Type memberType, MethodInfo? getMethod, FieldInfo? field)
        {
            var method = new DynamicMethod($"get_{field?.Name ?? getMethod!.Name}", typeof(object),
                new[] { typeof(object) }, owner.Module, skipVisibility: true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, owner);
            if (field != null)
            {
                il.Emit(OpCodes.Ldfld, field);
            }
            else
            {
                il.Emit(getMethod!.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getMethod);
            }

            if (memberType.IsValueType)
            {
                il.Emit(OpCodes.Box, memberType);
            }

            il.Emit(OpCodes.Ret);
            return (Func<object, object?>)method.CreateDelegate(typeof(Func<object, object?>));
        }

        private static Action<object, object?> EmitSetter(Type owner, Type memberType, MethodInfo? setMethod, FieldInfo? field)
        {
            var method = new DynamicMethod($"set_{field?.Name ?? setMethod!.Name}", null,
                new[] { typeof(object), typeof(object) }, owner.Module, skipVisibility: true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, owner);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(memberType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, memberType);
            if (field != null)
            {
                il.Emit(OpCodes.Stfld, field);
            }
            else
            {
                il.Emit(setMethod!.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setMethod);
            }

            il.Emit(OpCodes.Ret);
            return (Action<object, object?>)method.CreateDelegate(typeof(Action<object, object?>));
        }
    }
}
