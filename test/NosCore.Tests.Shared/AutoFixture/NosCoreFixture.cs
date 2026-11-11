//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Core.Configuration;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking.SessionGroup;
using NosCore.PathFinder.Heuristic;
using NosCore.PathFinder.Interfaces;

namespace NosCore.Tests.Shared.AutoFixture
{
    /// <summary>
    /// Builds the class under test with every constructor dependency it does not care about mocked,
    /// so a test only names the collaborators it sets up or verifies.
    /// </summary>
    /// <remarks>
    /// <c>Freeze&lt;Mock&lt;IDep&gt;&gt;()</c> before <c>Create&lt;Sut&gt;()</c> to set up or verify a dependency.
    /// Formula services from NosCore.Algorithm, the clock, the session registry, the localizers and the
    /// world configuration are real and shared by everything the fixture builds. Collections are empty.
    /// </remarks>
    public class NosCoreFixture : Fixture
    {
        public static readonly Instant Epoch = Instant.FromUtc(2021, 01, 01, 01, 01, 01);

        public NosCoreFixture()
        {
            TestMapping.EnsureConfigured();
            Customize(new AutoMoqCustomization { GenerateDelegates = true });
            Behaviors.Add(new EmptyMockDefaultsBehavior());
            Customizations.Add(new AlgorithmServiceRelay());
            Customizations.Add(new IdServiceRelay());
            RepeatCount = 0;

            var clock = new FakeClock(Epoch);
            this.Inject(clock);
            this.Inject<IClock>(clock);

            var sessionRegistry = new SessionRegistry(NullLogger<SessionRegistry>.Instance);
            this.Inject(sessionRegistry);
            this.Inject<ISessionRegistry>(sessionRegistry);

            this.Inject<ILoggerFactory>(NullLoggerFactory.Instance);
            this.Inject<IHeuristic>(new OctileDistanceHeuristic());
            this.Inject(Options.Create(new WorldConfiguration
            {
                Database = new(),
                MasterCommunication = new(),
                BackpackSize = 2,
                MaxItemAmount = 999,
                MaxSpPoints = 10_000,
                MaxAdditionalSpPoints = 1_000_000,
                MaxGoldAmount = 999_999_999
            }));

            this.Inject(TestLocalizers.Log);
            this.Inject(TestLocalizers.Game);

            var sessionGroupFactory = new Mock<ISessionGroupFactory>();
            sessionGroupFactory.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
            this.Inject(sessionGroupFactory.Object);
        }
    }
}
