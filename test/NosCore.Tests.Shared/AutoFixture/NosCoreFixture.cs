//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NosCore.GameObject.Services.BroadcastService;

namespace NosCore.Tests.Shared.AutoFixture
{
    /// <summary>
    /// Builds the class under test with every constructor dependency it does not care about mocked,
    /// so a test only names the collaborators it sets up or verifies.
    /// </summary>
    /// <remarks>
    /// <c>Freeze&lt;Mock&lt;IDep&gt;&gt;()</c> before <c>Create&lt;Sut&gt;()</c> to set up or verify a dependency.
    /// Formula services from NosCore.Algorithm and the state shared with <see cref="TestHelpers"/>
    /// sessions (clock, session registry, localizers, world configuration) are real, not mocked.
    /// </remarks>
    public class NosCoreFixture : Fixture
    {
        public NosCoreFixture()
        {
            Customize(new AutoMoqCustomization { GenerateDelegates = true });
            Behaviors.Add(new EmptyMockDefaultsBehavior());
            Customizations.Add(new AlgorithmServiceRelay());

            this.Register<IClock>(() => TestHelpers.Instance.Clock);
            this.Register(() => TestHelpers.Instance.Clock);
            this.Register<ISessionRegistry>(() => TestHelpers.Instance.SessionRegistry);
            this.Register(() => TestHelpers.Instance.SessionRegistry);
            this.Register(() => TestHelpers.Instance.LogLanguageLocalizer);
            this.Register(() => TestHelpers.Instance.GameLanguageLocalizer);
            this.Register(() => TestHelpers.Instance.WorldConfiguration);
            this.Register(() => TestHelpers.Instance.SessionGroupFactory);
            this.Register(() => TestHelpers.Instance.DistanceCalculator);
            this.Register<ILoggerFactory>(() => NullLoggerFactory.Instance);
        }
    }
}
