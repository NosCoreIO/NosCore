//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture.Kernel;
using Moq;
using System;
using System.Collections;

namespace NosCore.Tests.Shared.AutoFixture
{
    /// <summary>
    /// Un-setup mock members return a nested mock for a service interface and null or empty for
    /// anything else: a lookup that should miss must not find a proxy DTO.
    /// </summary>
    public class EmptyMockDefaultsBehavior : ISpecimenBuilderTransformation
    {
        public ISpecimenBuilderNode Transform(ISpecimenBuilder builder) =>
            new Postprocessor(builder, new UseServiceMockDefaults(), new TrueRequestSpecification());

        private sealed class UseServiceMockDefaults : ISpecimenCommand
        {
            public void Execute(object specimen, ISpecimenContext context)
            {
                if (specimen is Mock mock)
                {
                    mock.DefaultValueProvider = ServiceMockDefaultValueProvider.Instance;
                }
            }
        }

        private sealed class ServiceMockDefaultValueProvider : LookupOrFallbackDefaultValueProvider
        {
            public static readonly ServiceMockDefaultValueProvider Instance = new();

            protected override object? GetFallbackDefaultValue(Type type, Mock mock)
            {
                if (type.GetElementType() is { } arrayElement)
                {
                    return Array.CreateInstance(arrayElement, new int[type.GetArrayRank()]);
                }

                if (type.IsInterface && type.IsGenericType && type.GetGenericArguments() is [var element]
                    && type.IsAssignableFrom(element.MakeArrayType()))
                {
                    return Array.CreateInstance(element, 0);
                }

                if (type.IsInterface && !typeof(IEnumerable).IsAssignableFrom(type)
                    && Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type)) is Mock nested)
                {
                    nested.DefaultValueProvider = this;
                    return nested.Object;
                }

                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }
        }
    }
}
