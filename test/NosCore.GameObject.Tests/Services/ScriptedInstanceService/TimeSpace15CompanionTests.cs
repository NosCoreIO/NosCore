//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.TimeSpacePenaltyService;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Instances.Scripting;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    // Drives the shipped timespace-15.csx: its last room opens the talk offering the companion
    // that has followed the player since the second room.
    [TestClass]
    public class TimeSpace15CompanionTests
    {
        private const short Companion = 317;
        private const int CompanionTalk = 6194;
        private const short MapSize = 20;

        private TestWorld World;
        private ClientSession _session;
        private ScriptedInstanceRun _run;
        private TimeSpaceCompanionHandler _handler;
        private Dictionary<int, MapInstance> _rooms;
        private int _second;
        private int _last;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            var script = await new CsxInstanceScriptLoader(Path.Combine(AppContext.BaseDirectory, "instances"))
                .LoadAsync("timespace-15") ?? throw new AssertFailedException("timespace-15.csx did not load.");
            var monsters = new List<NpcMonsterDto> { new() { NpcMonsterVNum = Companion, Level = 24, MaxHp = 1730, MaxMp = 1730 } };
            var catalog = new NpcMonsterCatalog(monsters);
            World.Inject(monsters);
            World.Inject<IMateService>(new GameObject.Services.MateService.MateService(World.MateDao, catalog, new IdService<Mate>(1),
                World.Create<MateStatsService>(), NullLogger<GameObject.Services.MateService.MateService>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object));

            _rooms = script.Definition.Rooms.ToDictionary(room => room.Key, room => Room(room.VNum));
            _second = script.Definition.Rooms[1].Key;
            _last = script.Definition.Rooms[^1].Key;
            _run = new ScriptedInstanceRun(
                new TimeSpaceRunHost(
                    new ScriptedInstance { ScriptedInstanceId = 15, MapId = 1, PositionX = 9, PositionY = 7, LevelMinimum = 1, Logic = script },
                    new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), new Mock<IGameEventDispatcher>().Object,
                    World.Clock, World.Create<TimeSpacePenaltyService>()),
                script, _rooms, catalog, new RandomProvider(), World.Clock, World.SessionRegistry,
                World.Create<AggroService>(), World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(),
                NullLogger.Instance, World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            var instances = World.Freeze<Mock<IScriptedInstanceService>>();
            instances.Setup(s => s.GetRun(It.IsAny<Guid>())).Returns(_run);
            _handler = World.Create<TimeSpaceCompanionHandler>();

            _session = await World.CreateSessionAsync();
            await EnterAsync(script.Definition.Rooms[0].Key, 9, 7);
        }

        private MapInstance Room(short mapId)
        {
            var data = new byte[4 + MapSize * MapSize];
            data[0] = (byte)MapSize;
            data[2] = (byte)MapSize;
            return World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map { MapId = mapId, NameI18NKey = "timeSpaceRoom", Data = data },
                Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
        }

        [TestMethod]
        public async Task TheCompanionFollowsThePlayerToTheLastRoomAndJoinsThere()
        {
            await TheCompanionFollowsAsync();

            await AnswerAsync();

            var partner = _session.Character.Mates.Values.Single();
            Assert.AreEqual(Companion, partner.VNum);
            Assert.AreEqual(MateType.Partner, partner.MateType);
            Assert.IsFalse(_rooms[_last].EnumerateNpcs().Any(npc => npc.VNum == Companion));
        }

        [TestMethod]
        public async Task TheLastRoomStopsWaitingOnTheTalkOnceTheCompanionJoins()
        {
            await TheCompanionFollowsAsync();

            await AnswerAsync();
            _session.LastPackets.Clear();
            await EnterAsync(_last, 1, 9);

            Assert.IsFalse(_session.LastPackets.OfType<NpcReqPacket>().Any(talk => talk.Data == CompanionTalk));
        }

        private async Task TheCompanionFollowsAsync()
        {
            await EnterAsync(_second, 1, 9);
            await _run.TickAsync();
            await _run.DialogClosedAsync(_session.Character.CharacterId);
            _session.LastPackets.Clear();

            await EnterAsync(_last, 1, 9);

            Assert.AreEqual(1, _session.LastPackets.OfType<NpcReqPacket>().Count(talk => talk.Data == CompanionTalk));
            Assert.AreEqual(_session.Character.CharacterId,
                _rooms[_last].EnumerateNpcs().Single(npc => npc.VNum == Companion).LeaderId);
        }

        private async Task EnterAsync(int room, short x, short y)
        {
            var mapInstance = _rooms[room];
            _session.Character.MapInstance = mapInstance;
            _session.Character.PositionX = x;
            _session.Character.PositionY = y;
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, mapInstance.MapInstanceId);
            await _run.EnteredAsync(_session, mapInstance.MapInstanceId);
        }

        private Task AnswerAsync() =>
            _handler.HandleAsync(_session, _session.Character, new NrunPacket
            {
                Runner = NrunRunnerType.GetXCompanion,
                Type = Companion,
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId
            });
    }
}
