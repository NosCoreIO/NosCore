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
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class ReqInfoPacketHandlerTests
    {
        private ReqInfoPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TestHelpers.Instance.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = Session.Channel!.Id,
                SessionId = Session.SessionId,
                Sender = Session,
                AccountName = Session.Account.Name,
                Disconnect = () => Task.CompletedTask,
                CharacterId = Session.Character.CharacterId,
                MapInstanceId = Session.Character.MapInstance.MapInstanceId,
            });

            Handler = new ReqInfoPacketHandler(
                new Mock<ILogger>().Object,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task PlayerReqInfoRepliesWithTcInfoForTheTargetedCharacter()
        {
            await new Spec("req_info on a player target replies with tc_info")
                .WhenAsync(RequestingPlayerInfoForSelf)
                .Then(TcInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayerReqInfoForUnknownTargetEmitsNothing()
        {
            await new Spec("req_info on an unknown player visualId emits nothing")
                .WhenAsync(RequestingPlayerInfoForUnknownVisualId)
                .Then(NoTcInfoShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcReqInfoRepliesWithEInfoForNpcOnTheMap()
        {
            await new Spec("req_info 5 on an NPC present on the current map replies with e_info 2 ...")
                .Given(NpcIsOnMap)
                .WhenAsync(RequestingNpcInfo)
                .Then(EInfoNpcMonsterPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcReqInfoForUnknownTargetEmitsNothing()
        {
            await new Spec("req_info 5 on an unknown NPC visualId emits nothing")
                .WhenAsync(RequestingNpcInfo)
                .Then(NoEInfoNpcMonsterPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MateReqInfoForUnsupportedVisualTypeIsQuietNoOp()
        {
            // `req_info 6 <visualType> <visualId>` with an unknown discriminator (e.g. 1 =
            // player, 10 = mate-when-wired) is logged at Debug and drops.
            await new Spec("req_info 6 with unsupported visualType is a quiet no-op")
                .WhenAsync(RequestingMateInfoWithMateVNum)
                .Then(NoEInfoNpcMonsterPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MateReqInfoFallsBackToMonsterLookupWhenMateVNumAbsent()
        {
            // Observed in NosCore's client revision: right-clicking a monster emits
            // `req_info 6` with MateVNum unset. OpenNos stock routes this through the
            // default branch (tc_info / no-op); we instead look the monster up on the
            // current MapInstance and reply with e_info subtype-10 so the info card
            // populates for mobs.
            await new Spec("req_info 6 with MateVNum absent and a monster on the map emits e_info")
                .Given(MonsterIsOnMap)
                .WhenAsync(RequestingMateInfoForMonster)
                .Then(EInfoNpcMonsterPacketShouldBeSent)
                .ExecuteAsync();
        }

        // --- Givens ---

        private void MonsterIsOnMap()
        {
            Session.Character.MapInstance.LoadMonsters(
                new List<MapMonsterDto>
                {
                    new()
                    {
                        MapMonsterId = 300,
                        MapId = Session.Character.MapInstance.Map.MapId,
                        MapX = 3,
                        MapY = 3,
                        VNum = 1,
                    }
                },
                new List<NpcMonsterDto>
                {
                    new()
                    {
                        NpcMonsterVNum = 1,
                        Level = 3,
                        MaxHp = 100,
                        MaxMp = 50,
                        Name = new I18NString(),
                    }
                });
        }

        private void NpcIsOnMap()
        {
            Session.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto>
                {
                    new()
                    {
                        MapNpcId = 100,
                        MapId = Session.Character.MapInstance.Map.MapId,
                        MapX = 2,
                        MapY = 2,
                        VNum = 1,
                    }
                },
                new List<NpcMonsterDto>
                {
                    new()
                    {
                        NpcMonsterVNum = 1,
                        Level = 3,
                        MaxHp = 100,
                        MaxMp = 50,
                        Name = new I18NString(),
                    }
                });
        }

        // --- Whens ---

        private async Task RequestingPlayerInfoForSelf()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ReqInfoPacket
            {
                ReqType = ReqInfoType.PlayerInfo,
                TargetVNum = Session.Character.VisualId,
            }, Session);
        }

        private async Task RequestingPlayerInfoForUnknownVisualId()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ReqInfoPacket
            {
                ReqType = ReqInfoType.PlayerInfo,
                TargetVNum = 99999,
            }, Session);
        }

        private async Task RequestingNpcInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ReqInfoPacket
            {
                ReqType = ReqInfoType.NpcInfo,
                TargetVNum = 100,
            }, Session);
        }

        private async Task RequestingMateInfoWithMateVNum()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ReqInfoPacket
            {
                ReqType = ReqInfoType.MateInfo,
                TargetVNum = 1,
                MateVNum = 1,
            }, Session);
        }

        private async Task RequestingMateInfoForMonster()
        {
            // Observed wire layout from a live session: `req_info 6 3 <mapMonsterId>` —
            // TargetVNum carries the VisualType discriminator (3 = Monster), MateVNum
            // carries the VisualId.
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ReqInfoPacket
            {
                ReqType = ReqInfoType.MateInfo,
                TargetVNum = (long)VisualType.Monster,
                MateVNum = 300,
            }, Session);
        }

        // --- Thens ---

        private void TcInfoPacketShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.Any(p => p is TcInfoPacket));

        private void NoTcInfoShouldBeSent() =>
            Assert.IsFalse(Session.LastPackets.Any(p => p is TcInfoPacket));

        private void EInfoNpcMonsterPacketShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.Any(p => p is EInfoNpcMonsterPacket pkt && pkt.NpcMonsterVNum == 1 && pkt.Level == 3));

        private void NoEInfoNpcMonsterPacketShouldBeSent() =>
            Assert.IsFalse(Session.LastPackets.Any(p => p is EInfoNpcMonsterPacket));
    }
}
