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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Inventory;
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
        private List<NpcMonsterDto> NpcMonsters = null!;

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

            NpcMonsters = new List<NpcMonsterDto>
            {
                new()
                {
                    NpcMonsterVNum = 303,
                    Level = 35,
                    MaxHp = 1360,
                    MaxMp = 630,
                    Name = new I18NString(),
                },
            };

            Handler = new ReqInfoPacketHandler(
                new Mock<ILogger>().Object,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry,
                NpcMonsters);
        }

        [TestMethod]
        public async Task PlayerReqInfoRepliesWithTcInfoForTheTargetedCharacter()
        {
            await new Spec("req_info 1 <characterId> replies with tc_info for the targeted player")
                .WhenAsync(RequestingPlayerInfoForSelf)
                .Then(TcInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayerReqInfoForUnknownTargetEmitsNothing()
        {
            await new Spec("req_info 1 <unknownId> emits nothing")
                .WhenAsync(RequestingPlayerInfoForUnknownVisualId)
                .Then(NoTcInfoShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcInfoReqResolvesTemplateByVNumFromTheGlobalCatalog()
        {
            // Official wire: `req_info 5 303` -> `e_info 10 303 35 ...`. Type 5 is a static
            // catalog lookup on the NpcMonster VNum, NOT a lookup on entities currently on
            // the map — so the reply comes back even if no such NPC is spawned nearby.
            await new Spec("req_info 5 <npcMonsterVNum> replies with e_info for the catalog entry")
                .WhenAsync(RequestingNpcInfoByVNum)
                .Then(EInfoForVNum303ShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcInfoReqForUnknownVNumEmitsNothing()
        {
            await new Spec("req_info 5 <unknownVNum> emits nothing")
                .WhenAsync(RequestingNpcInfoForUnknownVNum)
                .Then(NoEInfoNpcMonsterPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MapEntityInfoReqResolvesNpcByVisualIdOnCurrentMap()
        {
            // Official wire: `req_info 6 2 <mapNpcVisualId>` -> `e_info 10 <npcVNum> ...`.
            // Type 6 is live-map lookup discriminated by VisualType in TargetVNum and the
            // VisualId in MateVNum.
            await new Spec("req_info 6 2 <mapNpcVisualId> replies with e_info for the npc on the current map")
                .Given(NpcIsOnMap)
                .WhenAsync(RequestingMapInfoForNpc)
                .Then(EInfoForVNum909ShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MapEntityInfoReqResolvesMonsterByVisualIdOnCurrentMap()
        {
            // Official wire: `req_info 6 3 <mapMonsterVisualId>` -> `e_info 10 <vnum> ...`.
            await new Spec("req_info 6 3 <mapMonsterVisualId> replies with e_info for the monster on the current map")
                .Given(MonsterIsOnMap)
                .WhenAsync(RequestingMapInfoForMonster)
                .Then(EInfoForVNum25ShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MapEntityInfoReqWithoutVisualIdIsQuietNoOp()
        {
            await new Spec("req_info 6 without a VisualId (MateVNum absent) is a quiet no-op")
                .WhenAsync(RequestingMapInfoWithMissingVisualId)
                .Then(NoEInfoNpcMonsterPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MapEntityInfoReqForUnsupportedVisualTypeIsQuietNoOp()
        {
            // VisualType.Player via req_info 6 is currently unwired — the handler drops at
            // Debug level instead of spamming WARN.
            await new Spec("req_info 6 1 <playerId> is a quiet no-op (player branch not wired)")
                .WhenAsync(RequestingMapInfoForPlayer)
                .Then(NoEInfoNpcMonsterPacketShouldBeSent)
                .ExecuteAsync();
        }

        // --- Givens ---

        private void NpcIsOnMap()
        {
            Session.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto>
                {
                    new()
                    {
                        MapNpcId = 73680,
                        MapId = Session.Character.MapInstance.Map.MapId,
                        MapX = 34,
                        MapY = 122,
                        VNum = 909,
                    }
                },
                new List<NpcMonsterDto>
                {
                    new()
                    {
                        NpcMonsterVNum = 909,
                        Level = 71,
                        MaxHp = 21450,
                        MaxMp = 1863,
                        Name = new I18NString(),
                    }
                });
        }

        private void MonsterIsOnMap()
        {
            Session.Character.MapInstance.LoadMonsters(
                new List<MapMonsterDto>
                {
                    new()
                    {
                        MapMonsterId = 2848,
                        MapId = Session.Character.MapInstance.Map.MapId,
                        MapX = 3,
                        MapY = 3,
                        VNum = 25,
                    }
                },
                new List<NpcMonsterDto>
                {
                    new()
                    {
                        NpcMonsterVNum = 25,
                        Level = 2,
                        MaxHp = 175,
                        MaxMp = 15,
                        Name = new I18NString(),
                    }
                });
        }

        // --- Whens ---

        private Task RequestingPlayerInfoForSelf() => ExecuteAsync(new ReqInfoPacket
        {
            ReqType = ReqInfoType.PlayerInfo,
            TargetVNum = Session.Character.VisualId,
        });

        private Task RequestingPlayerInfoForUnknownVisualId() => ExecuteAsync(new ReqInfoPacket
        {
            ReqType = ReqInfoType.PlayerInfo,
            TargetVNum = 99999,
        });

        private Task RequestingNpcInfoByVNum() => ExecuteAsync(new ReqInfoPacket
        {
            ReqType = ReqInfoType.NpcInfo,
            TargetVNum = 303,
        });

        private Task RequestingNpcInfoForUnknownVNum() => ExecuteAsync(new ReqInfoPacket
        {
            ReqType = ReqInfoType.NpcInfo,
            TargetVNum = 99999,
        });

        private Task RequestingMapInfoForNpc() => ExecuteAsync(new ReqInfoPacket
        {
            ReqType = ReqInfoType.MateInfo,
            TargetVNum = (long)VisualType.Npc,
            MateVNum = 73680,
        });

        private Task RequestingMapInfoForMonster() => ExecuteAsync(new ReqInfoPacket
        {
            ReqType = ReqInfoType.MateInfo,
            TargetVNum = (long)VisualType.Monster,
            MateVNum = 2848,
        });

        private Task RequestingMapInfoWithMissingVisualId() => ExecuteAsync(new ReqInfoPacket
        {
            ReqType = ReqInfoType.MateInfo,
            TargetVNum = (long)VisualType.Monster,
            MateVNum = null,
        });

        private Task RequestingMapInfoForPlayer() => ExecuteAsync(new ReqInfoPacket
        {
            ReqType = ReqInfoType.MateInfo,
            TargetVNum = (long)VisualType.Player,
            MateVNum = (int)Session.Character.VisualId,
        });

        private Task ExecuteAsync(ReqInfoPacket packet)
        {
            Session.LastPackets.Clear();
            return Handler.ExecuteAsync(packet, Session);
        }

        // --- Thens ---

        private void TcInfoPacketShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.Any(p => p is TcInfoPacket));

        private void NoTcInfoShouldBeSent() =>
            Assert.IsFalse(Session.LastPackets.Any(p => p is TcInfoPacket));

        private void EInfoForVNum303ShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.Any(p => p is EInfoNpcMonsterPacket pkt && pkt.NpcMonsterVNum == 303 && pkt.Level == 35));

        private void EInfoForVNum909ShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.Any(p => p is EInfoNpcMonsterPacket pkt && pkt.NpcMonsterVNum == 909 && pkt.Level == 71));

        private void EInfoForVNum25ShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.Any(p => p is EInfoNpcMonsterPacket pkt && pkt.NpcMonsterVNum == 25 && pkt.Level == 2));

        private void NoEInfoNpcMonsterPacketShouldBeSent() =>
            Assert.IsFalse(Session.LastPackets.Any(p => p is EInfoNpcMonsterPacket));
    }
}
