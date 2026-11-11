//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture.Kernel;
using NosCore.Core.Services.IdService;
using System;

namespace NosCore.Tests.Shared.AutoFixture
{
    /// <summary>
    /// Resolves an <see cref="IIdService{T}"/> to a real one starting at 1, since a mocked one hands
    /// out the same id to every entity.
    /// </summary>
    public class IdServiceRelay : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context) =>
            request is Type { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(IIdService<>)
                ? Activator.CreateInstance(typeof(IdService<>).MakeGenericType(type.GetGenericArguments()), 1L) ?? new NoSpecimen()
                : new NoSpecimen();
    }
}
