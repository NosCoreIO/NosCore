//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture.Kernel;
using NosCore.Algorithm.ExperienceService;
using System;
using System.Linq;
using System.Reflection;

namespace NosCore.Tests.Shared.AutoFixture
{
    /// <summary>
    /// Resolves a NosCore.Algorithm service interface to its implementation, since a mocked formula
    /// returns zero for every curve.
    /// </summary>
    public class AlgorithmServiceRelay : ISpecimenBuilder
    {
        private static readonly Assembly AlgorithmAssembly = typeof(IExperienceService).Assembly;

        public object Create(object request, ISpecimenContext context)
        {
            if (request is not Type { IsInterface: true } type || type.Assembly != AlgorithmAssembly)
            {
                return new NoSpecimen();
            }

            var implementation = AlgorithmAssembly.GetTypes()
                .SingleOrDefault(t => t is { IsClass: true, IsAbstract: false } && type.IsAssignableFrom(t));
            return implementation is null ? new NoSpecimen() : context.Resolve(implementation);
        }
    }
}
