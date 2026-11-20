//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class BuffLineTests
    {
        private static readonly CardDto Card = new() { CardId = 684, Duration = 100 };

        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _buffs = World.Create<BuffService>();
        }

        [TestMethod]
        public async Task ACardLandingIsAnnouncedToItsBearer()
        {
            await _buffs.ApplyAsync(_session.Character, Card, [], null);

            PacketAssert.AreEqual(Line(Game18NConstString.UnderEffect), Lines().Single());
        }

        [TestMethod]
        public async Task ACardAppliedAgainWhileItRunsIsNotAnnouncedAgain()
        {
            await _buffs.ApplyAsync(_session.Character, Card, [], null);
            _session.LastPackets.Clear();

            await _buffs.ApplyAsync(_session.Character, Card, [], null);

            Assert.AreEqual(0, Lines().Count);
        }

        [TestMethod]
        public async Task ACardRunningOutIsAnnouncedToItsBearer()
        {
            await _buffs.ApplyAsync(_session.Character, Card, [], null);
            _session.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(10));
            await _buffs.TickAsync(_session.Character);

            PacketAssert.AreEqual(Line(Game18NConstString.LostSideEffect), Lines().Single());
        }

        [TestMethod]
        public async Task ACardTakenOffIsAnnouncedToItsBearer()
        {
            await _buffs.ApplyAsync(_session.Character, Card, [], null);
            _session.LastPackets.Clear();

            await _buffs.RemoveAsync(_session.Character, Card.CardId);

            PacketAssert.AreEqual(Line(Game18NConstString.LostSideEffect), Lines().Single());
        }

        [TestMethod]
        public async Task CardsClearedByADeathGoWithoutALine()
        {
            await _buffs.ApplyAsync(_session.Character, Card, [], null);
            _session.LastPackets.Clear();

            await _buffs.ClearAsync(_session.Character);

            Assert.AreEqual(0, Lines().Count);
        }

        private List<SayiPacket> Lines() =>
            _session.LastPackets.OfType<SayiPacket>().ToList();

        private SayiPacket Line(Game18NConstString message) => new()
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = SayColorType.WhiteTimeout,
            Message = message,
            ArgumentType = 3,
            Game18NArguments = { Card.CardId.ToString() },
        };
    }
}
