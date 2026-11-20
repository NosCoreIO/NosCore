//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.TimeSpacePenaltyService;
using NosCore.Core.Services.IdService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class TimeSpaceCompanionHandlerTests
    {
        private const short Companion = 317;
        private const int CompanionTalk = 6194;
        private const byte CompanionLevel = 24;

        private sealed class Script : InstanceScript
        {
            public List<string> Calls { get; } = [];

            public Script() => Definition = new ScriptedInstanceDefinition { Id = 15 };

            public override void OnFirstEnter(IInstanceRun run)
            {
                run.SummonNpcs(1, new NpcSpawn(Companion, new Spot(2, 2), IsEscort: true));
                run.Dialog(1, CompanionTalk);
            }

            public override void OnDialogClosed(IInstanceRun run, int room, int dialog) =>
                Calls.Add($"closed({room},{dialog})");
        }

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _room;
        private Script _script;
        private ScriptedInstanceRun _run;
        private TimeSpaceCompanionHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            var monsters = new List<NpcMonsterDto>
            {
                new() { NpcMonsterVNum = Companion, Level = CompanionLevel, MaxHp = 1730, MaxMp = 1730 }
            };
            var catalog = new NpcMonsterCatalog(monsters);
            World.Inject(monsters);
            World.Inject<IMateService>(new MateService(World.MateDao, catalog, new IdService<Mate>(1),
                World.Create<MateStatsService>(), NullLogger<MateService>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object));

            _room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 4004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            _script = new Script();
            _run = new ScriptedInstanceRun(
                new TimeSpaceRunHost(
                    new ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, PositionX = 5, PositionY = 6, LevelMinimum = 1, Logic = _script },
                    new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), new Mock<IGameEventDispatcher>().Object,
                    World.Clock, World.Create<TimeSpacePenaltyService>()),
                _script, new Dictionary<int, MapInstance> { [1] = _room }, catalog, new RandomProvider(), World.Clock,
                World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(),
                NullLogger.Instance, World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            World.Freeze<Mock<IScriptedInstanceService>>()
                .Setup(s => s.GetRun(_room.MapInstanceId)).Returns(_run);
            _handler = World.Create<TimeSpaceCompanionHandler>();

            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = _room;
            _session.Character.PositionX = 1;
            _session.Character.PositionY = 1;
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, _room.MapInstanceId);
        }

        [TestMethod]
        public void TheHandlerAnswersTheCompanionButton() =>
            Assert.AreEqual(NrunRunnerType.GetXCompanion, _handler.Runner);

        [TestMethod]
        public async Task AnsweringTheTalkMakesTheCompanionALastingPartner()
        {
            await TalkOpenAsync();

            await AnswerAsync(Companion);

            var partner = _session.Character.Mates.Values.Single();
            Assert.AreEqual(Companion, partner.VNum);
            Assert.AreEqual(MateType.Partner, partner.MateType);
            Assert.AreEqual(CompanionLevel, partner.Level);
            Assert.IsFalse(partner.IsTeamMember);
            var stored = World.MateDao.LoadAll().Single();
            Assert.AreEqual(_session.Character.CharacterId, stored.CharacterId);
            Assert.AreEqual(Companion, stored.VNum);
            Assert.AreEqual(MateType.Partner, stored.MateType);

            PacketAssert.AreEqual(new CtlPacket { Type = VisualType.Npc, PetId = partner.MateTransportId, Action = 3, Unknown = 0 },
                _session.LastPackets.OfType<CtlPacket>().Single(p => p.PetId == partner.MateTransportId));
            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.SomeoneJoinGroup,
                ArgumentType = 8,
                Game18NArguments = { Companion }
            }, _session.LastPackets.OfType<SayiPacket>().Single());
            Assert.IsTrue(_session.LastPackets.OfType<PclearPacket>().Any());
            Assert.AreEqual(Companion, _session.LastPackets.OfType<ScnPacket>().Single().NpcMonsterVNum);
        }

        [TestMethod]
        public async Task TheCompanionLeavesTheRoom()
        {
            await TalkOpenAsync();
            var companionId = _room.EnumerateNpcs().Single().VisualId;

            await AnswerAsync(Companion);

            Assert.IsFalse(_room.EnumerateNpcs().Any());
            PacketAssert.AreEqual(new OutPacket { VisualType = VisualType.Npc, VisualId = companionId },
                _room.LastPackets.OfType<OutPacket>().Single());
        }

        [TestMethod]
        public async Task TheAnswerClosesTheTalkForTheScript()
        {
            await TalkOpenAsync();

            await AnswerAsync(Companion);

            CollectionAssert.AreEqual(new[] { $"closed(1,{CompanionTalk})" }, _script.Calls);
        }

        [TestMethod]
        public async Task AClosedTalkIsNotShownAgainOnReturn()
        {
            await TalkOpenAsync();
            await AnswerAsync(Companion);
            _session.LastPackets.Clear();

            await _run.EnteredAsync(_session, _room.MapInstanceId);

            Assert.IsFalse(_session.LastPackets.OfType<NpcReqPacket>().Any());
        }

        [TestMethod]
        public async Task APartnerAlreadyOwnedIsRefusedAndStaysInTheRoom()
        {
            var owned = await World.MateDao.TryInsertOrUpdateAsync(new MateDto
            {
                CharacterId = _session.Character.CharacterId, VNum = Companion, MateType = MateType.Partner, Level = 40
            }) ?? throw new AssertFailedException("The owned partner was not stored.");
            var mate = World.Create<IMateService>().Adopt(owned, new NpcMonsterDto { NpcMonsterVNum = Companion }, []);
            _session.Character.Mates[mate.MateTransportId] = mate;
            await TalkOpenAsync();

            await AnswerAsync(Companion);

            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.AlreadyInYourParty
            }, _session.LastPackets.OfType<SayiPacket>().Single());
            Assert.AreEqual(1, _session.Character.Mates.Count);
            Assert.AreEqual(1, World.MateDao.LoadAll().Count());
            Assert.AreEqual(1, _room.EnumerateNpcs().Count());
            Assert.IsFalse(_room.LastPackets.OfType<OutPacket>().Any());
        }

        [TestMethod]
        public async Task WithoutTheTalkOpenTheButtonDoesNothing()
        {
            await _run.EnteredAsync(_session, _room.MapInstanceId);
            await _run.DialogClosedAsync(_session.Character.CharacterId);

            await AnswerAsync(Companion);

            Assert.AreEqual(0, _session.Character.Mates.Count);
            Assert.AreEqual(1, _room.EnumerateNpcs().Count());
        }

        [TestMethod]
        public async Task AnNpcTheRoomDoesNotHoldIsNotTaken()
        {
            await TalkOpenAsync();

            await AnswerAsync(318);

            Assert.AreEqual(0, _session.Character.Mates.Count);
            Assert.AreEqual(1, _room.EnumerateNpcs().Count());
            Assert.IsFalse(_script.Calls.Any());
        }

        [TestMethod]
        public async Task AnEscortLedByAnotherPlayerIsNotTaken()
        {
            await TalkOpenAsync();
            var companion = _room.EnumerateNpcs().Single();
            companion.LeaderId = _session.Character.CharacterId + 1;

            await AnswerAsync(Companion);

            Assert.AreEqual(0, _session.Character.Mates.Count);
            Assert.AreEqual(1, _room.EnumerateNpcs().Count());
        }

        [TestMethod]
        public async Task AnAnswerFromAnotherPlayersTalkIsIgnored()
        {
            await TalkOpenAsync();

            await _handler.HandleAsync(_session, null, new NrunPacket
            {
                Runner = NrunRunnerType.GetXCompanion, Type = Companion, VisualType = VisualType.Npc, VisualId = 1
            });

            Assert.AreEqual(0, _session.Character.Mates.Count);
            Assert.AreEqual(1, _room.EnumerateNpcs().Count());
        }

        private async Task TalkOpenAsync()
        {
            await _run.EnteredAsync(_session, _room.MapInstanceId);
            _session.LastPackets.Clear();
            _room.LastPackets.Clear();
        }

        private Task AnswerAsync(short vNum) =>
            _handler.HandleAsync(_session, _session.Character, new NrunPacket
            {
                Runner = NrunRunnerType.GetXCompanion,
                Type = vNum,
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId
            });
    }
}
