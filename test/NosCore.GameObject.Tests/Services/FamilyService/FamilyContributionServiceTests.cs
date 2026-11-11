//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyContributionServiceTests
    {
        private const long FamilyId = 3;
        private const long CharacterId = 7;

        private readonly List<(FamilyLogType Type, object[] Arguments)> _logs = new();
        private readonly List<IMessage> _published = new();
        private Mock<IFamilyHub> _hub;
        private FamilyContributionService _service;

        [TestInitialize]
        public void Setup()
        {
            _logs.Clear();
            _published.Clear();
            var logs = new Mock<IFamilyLogService>();
            logs.Setup(s => s.AddAsync(FamilyId, It.IsAny<FamilyLogType>(), It.IsAny<object[]>()))
                .Callback<long, FamilyLogType, object[]>((_, type, arguments) => _logs.Add((type, arguments)))
                .Returns(Task.CompletedTask);
            _hub = new Mock<IFamilyHub>();
            _hub.Setup(s => s.AddExperienceAsync(FamilyId, CharacterId, It.IsAny<int>()))
                .ReturnsAsync(new FamilyExperienceGain(1, false));
            var pubSub = new Mock<IPubSubHub>();
            pubSub.Setup(s => s.SendMessageAsync(It.IsAny<IMessage>())).Callback<IMessage>(_published.Add).ReturnsAsync(true);
            _service = new FamilyContributionService(logs.Object, _hub.Object, pubSub.Object);
        }

        [TestMethod]
        public async Task ReachingATenthLevelAbove20GivesTwentyTimesTheLevel()
        {
            await _service.CharacterLevelledUpAsync(FamilyId, CharacterId, "Tester", 29, 30);

            _hub.Verify(s => s.AddExperienceAsync(FamilyId, CharacterId, 600), Times.Once);
            var log = _logs.Single(s => s.Type == FamilyLogType.FamilyXp);
            CollectionAssert.AreEqual(new object[] { "Tester", 600 }, log.Arguments);
            Assert.IsNull(((FamilyChangedData)_published.Single()).ReachedFamilyLevel);
        }

        [TestMethod]
        public async Task OtherLevelsGiveNothing()
        {
            await _service.CharacterLevelledUpAsync(FamilyId, CharacterId, "Tester", 18, 20);
            await _service.CharacterLevelledUpAsync(FamilyId, CharacterId, "Tester", 30, 31);

            _hub.Verify(s => s.AddExperienceAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public async Task AFamilyLevelUpIsLoggedAndAnnounced()
        {
            _hub.Setup(s => s.AddExperienceAsync(FamilyId, CharacterId, 800)).ReturnsAsync(new FamilyExperienceGain(4, true));

            await _service.CharacterLevelledUpAsync(FamilyId, CharacterId, "Tester", 39, 40);

            var log = _logs.Single(s => s.Type == FamilyLogType.FamilyLevelUp);
            CollectionAssert.AreEqual(new object[] { (byte)4 }, log.Arguments);
            Assert.AreEqual((byte)4, ((FamilyChangedData)_published.Single()).ReachedFamilyLevel);
        }
    }
}
