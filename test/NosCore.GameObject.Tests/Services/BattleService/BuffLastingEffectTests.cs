//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Shared.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class BuffLastingEffectTests
    {
        private static readonly CardDto Wings = new() { CardId = 4002, Duration = 3000, HasLastingEffect = true };
        private static readonly CardDto Bleeding = new() { CardId = 56, Duration = 600 };

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _map;
        private BuffService _buffs;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _map = _session.Character.MapInstance;
            World.Freeze<Mock<IGameEventDispatcher>>();
            _buffs = World.Create<BuffService>();
        }

        [TestMethod]
        public async Task ACardWithALastingEffectStartsItOverForTheMap()
        {
            _map.LastPackets.Clear();

            await _buffs.ApplyAsync(_session.Character, Wings, Array.Empty<BCardDto>(), null);

            var effects = _map.LastPackets.OfType<BfePacket>().ToList();
            Assert.AreEqual(2, effects.Count);
            PacketAssert.AreEqual(Effect(0), effects[0]);
            PacketAssert.AreEqual(Effect(3000), effects[1]);
        }

        [TestMethod]
        public async Task ACardWithoutALastingEffectHasNone()
        {
            _map.LastPackets.Clear();

            await _buffs.ApplyAsync(_session.Character, Bleeding, Array.Empty<BCardDto>(), null);
            World.Clock.Advance(Duration.FromSeconds(61));
            await _buffs.TickAsync(_session.Character);

            Assert.IsFalse(_map.LastPackets.OfType<BfePacket>().Any());
        }

        [TestMethod]
        public async Task TheEffectEndsWithItsCard()
        {
            await _buffs.ApplyAsync(_session.Character, Wings, Array.Empty<BCardDto>(), null);
            _map.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(301));
            await _buffs.TickAsync(_session.Character);

            PacketAssert.AreEqual(Effect(0), _map.LastPackets.OfType<BfePacket>().Single());
        }

        [TestMethod]
        public async Task TheEffectEndsWhenItsCardIsTakenOff()
        {
            await _buffs.ApplyAsync(_session.Character, Wings, Array.Empty<BCardDto>(), null);
            _map.LastPackets.Clear();

            await _buffs.RemoveAsync(_session.Character, Wings.CardId);

            PacketAssert.AreEqual(Effect(0), _map.LastPackets.OfType<BfePacket>().Single());
        }

        [TestMethod]
        public async Task AClientThatStartsSeeingTheBearerGetsTheTimeLeft()
        {
            await _buffs.ApplyAsync(_session.Character, Wings, Array.Empty<BCardDto>(), null);
            await _buffs.ApplyAsync(_session.Character, Bleeding, Array.Empty<BCardDto>(), null);
            World.Clock.Advance(Duration.FromSeconds(36));

            var effects = _session.Character.GenerateLastingEffects(World.Clock.GetCurrentInstant()).ToList();

            PacketAssert.AreEqual(Effect(2640), effects.Single());
        }

        private BfePacket Effect(short remaining) => new()
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.VisualId,
            CardId = Wings.CardId,
            RemainingTime = remaining,
        };
    }
}
