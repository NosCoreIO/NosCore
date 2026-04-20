//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class NcifPacketHandlerTests
    {
        private NcifPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            var logger = new Mock<ILogger>().Object;
            Handler = new NcifPacketHandler(
                logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task NcifForPlayerShouldReturnStatInfo()
        {
            await new Spec("Ncif for player should return stat info")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingPlayerStatInfo)
                .Then(StInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForUnknownTypeShouldNotSendPacket()
        {
            await new Spec("Ncif for unknown type should not send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingUnknownTypeStatInfo)
                .Then(NoStInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForNonExistentEntityShouldNotSendPacket()
        {
            await new Spec("Ncif for non existent entity should not send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingNonExistentEntityStatInfo)
                .Then(NoStInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForMonsterShouldReturnStatInfoWithMonsterIdAndPercentages()
        {
            await new Spec("Ncif for monster returns its level + HP/MP percentages")
                .Given(CharacterIsOnMap)
                .And(MonsterIsOnMap)
                .WhenAsync(RequestingMonsterStatInfo)
                .Then(StInfoPacketShouldBeSent)
                .And(LastStPacketShouldHaveType_, VisualType.Monster)
                .And(LastStPacketShouldHaveHpPercentage_, 100)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForNpcShouldReturnStatInfo()
        {
            await new Spec("Ncif for NPC returns its level + HP/MP percentages")
                .Given(CharacterIsOnMap)
                .And(NpcIsOnMap)
                .WhenAsync(RequestingNpcStatInfo)
                .Then(StInfoPacketShouldBeSent)
                .And(LastStPacketShouldHaveType_, VisualType.Npc)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForPlayerWithZeroMaxHpReturnsZeroPercentageInsteadOfCrashing()
        {
            // Regression: GenerateStatInfo used to do `Hp / (float)MaxHp * 100` and produce
            // ±Infinity (cast to int.MinValue) when MaxHp was 0. Should clamp to 0 instead.
            await new Spec("Player stat info with MaxHp=0 returns 0 hp percentage instead of crashing")
                .Given(CharacterIsOnMap)
                .And(CharacterMaxHpAndMaxMpAreZero)
                .WhenAsync(RequestingPlayerStatInfo)
                .Then(StInfoPacketShouldBeSent)
                .And(LastStPacketShouldHaveHpPercentage_, 0)
                .And(LastStPacketShouldHaveMpPercentage_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifSelfTargetingResolvesViaSessionRegistry()
        {
            // Self-inspect: a player right-clicks their own portrait. The requesting
            // player must be findable via the same SessionRegistry path other players use.
            await new Spec("Self-targeted ncif resolves the requesting player and replies with stat info")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingOwnStatInfo)
                .Then(StInfoPacketShouldBeSent)
                .And(LastStPacketShouldHaveType_, VisualType.Player)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForObjectVisualTypeIsRejectedSilently()
        {
            // Dropped items use VisualType.Object — they are not statful entities,
            // so the handler should fall through to the default UNKNOWN branch
            // and emit no StPacket. Documents the current contract.
            await new Spec("Object type (dropped item) is rejected — items are not statful")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingObjectStatInfo)
                .Then(NoStInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private async Task RequestingOwnStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Player,
                TargetId = Session.Character.VisualId
            }, Session);
        }

        private async Task RequestingObjectStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Object,
                TargetId = 1
            }, Session);
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            TestHelpers.Instance.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = Session.Channel!.Id,
                SessionId = Session.SessionId,
                Sender = Session,
                AccountName = Session.Account.Name,
                Disconnect = () => Task.CompletedTask,
                CharacterId = Session.Character.CharacterId,
                MapInstanceId = Session.Character.MapInstance.MapInstanceId
            });
        }

        private async Task RequestingPlayerStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Player,
                TargetId = Session.Character.VisualId
            }, Session);
        }

        private async Task RequestingUnknownTypeStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = (VisualType)99,
                TargetId = 1
            }, Session);
        }

        private async Task RequestingNonExistentEntityStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Player,
                TargetId = 99999
            }, Session);
        }

        private void StInfoPacketShouldBeSent()
        {
            Assert.IsTrue(Session.LastPackets.Any(p => p is StPacket));
        }

        private void NoStInfoPacketShouldBeSent()
        {
            Assert.IsFalse(Session.LastPackets.Any(p => p is StPacket));
        }

        private StPacket LastStPacket() => Session.LastPackets.OfType<StPacket>().Last();

        private void LastStPacketShouldHaveType_(VisualType expected) =>
            Assert.AreEqual(expected, LastStPacket().Type);

        private void LastStPacketShouldHaveHpPercentage_(int expected) =>
            Assert.AreEqual(expected, LastStPacket().HpPercentage);

        private void LastStPacketShouldHaveMpPercentage_(int expected) =>
            Assert.AreEqual(expected, LastStPacket().MpPercentage);

        private void MonsterIsOnMap()
        {
            Session.Character.MapInstance.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 200, MapId = 0, MapX = 1, MapY = 1, VNum = 1 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 } });
        }

        private void NpcIsOnMap()
        {
            Session.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto> { new() { MapNpcId = 300, MapId = 0, MapX = 2, MapY = 2, VNum = 1 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 3 } });
        }

        private void CharacterMaxHpAndMaxMpAreZero()
        {
            Session.Character.MaxHp = 0;
            Session.Character.MaxMp = 0;
        }

        private async Task RequestingMonsterStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Monster,
                TargetId = 200
            }, Session);
        }

        private async Task RequestingNpcStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Npc,
                TargetId = 300
            }, Session);
        }
    }
}
