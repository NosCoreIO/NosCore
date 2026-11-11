//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture;
using AutoFixture.Kernel;

namespace NosCore.Tests.Shared.AutoFixture
{
    public static class FixtureExtensions
    {
        /// <summary>
        /// Resolves <typeparamref name="TService"/> to a real <typeparamref name="TImplementation"/>,
        /// itself built from the fixture, instead of a mock.
        /// </summary>
        public static IFixture UseReal<TService, TImplementation>(this IFixture fixture)
            where TImplementation : TService
        {
            fixture.Customizations.Add(new TypeRelay(typeof(TService), typeof(TImplementation)));
            return fixture;
        }
    }
}
