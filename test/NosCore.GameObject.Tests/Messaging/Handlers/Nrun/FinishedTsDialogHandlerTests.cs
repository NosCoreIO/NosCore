//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class FinishedTsDialogHandlerTests
    {
        private const int Talk = 6055;

        private sealed class Script : InstanceScript
        {
            public List<string> Calls { get; } = [];

            public Script() => Definition = new ScriptedInstanceDefinition { Id = 10 };

            public override void OnFirstEnter(IInstanceRun run) => run.Dialog(1, Talk);

            public override void OnDialogClosed(IInstanceRun run, int room, int dialog) =>
                Calls.Add($"closed({room},{dialog})");
        }

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _room;
        private Script _script;
        private ScriptedInstanceRun _run;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            var catalog = new NpcMonsterCatalog(new List<NpcMonsterDto>());
            _room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 4107,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            _script = new Script();
            _run = new ScriptedInstanceRun(
                World.CreateTimeSpaceRunHost(new ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, PositionX = 5, PositionY = 6, LevelMinimum = 1, Logic = _script }),
                _script, new Dictionary<int, MapInstance> { [1] = _room }, catalog, new RandomProvider(), World.Clock,
                World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(),
                NullLogger.Instance, World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            World.Freeze<Mock<IScriptedInstanceService>>()
                .Setup(s => s.GetRun(_room.MapInstanceId)).Returns(_run);

            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = _room;
            _session.Character.PositionX = 1;
            _session.Character.PositionY = 1;
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, _room.MapInstanceId);
            await _run.EnteredAsync(_session, _room.MapInstanceId);
        }

        [TestMethod]
        public async Task TheFirstEndOfTalkClosesTheTimeSpaceTalk()
        {
            await World.Create<FinishedTsDialogHandler>().HandleAsync(_session, null, Close(NrunRunnerType.FinishedTSDialog));

            CollectionAssert.AreEqual(new[] { $"closed(1,{Talk})" }, _script.Calls);
        }

        [TestMethod]
        public async Task TheSecondEndOfTalkClosesTheTimeSpaceTalk()
        {
            await World.Create<FinishedTsDialog2Handler>().HandleAsync(_session, null, Close(NrunRunnerType.FinishedTSDialog2));

            CollectionAssert.AreEqual(new[] { $"closed(1,{Talk})" }, _script.Calls);
        }

        [TestMethod]
        public async Task TheEndOfRunTalksCloseTheTimeSpaceTalk()
        {
            await World.Create<FinishedTsHandler>().HandleAsync(_session, null, Close(NrunRunnerType.FinishedTs));
            await World.Create<FinishedTs2Handler>().HandleAsync(_session, null, Close(NrunRunnerType.FinishedTS2));

            CollectionAssert.AreEqual(new[] { $"closed(1,{Talk})" }, _script.Calls);
        }

        private NrunPacket Close(NrunRunnerType runner) => new()
        {
            Runner = runner,
            Type = 0,
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId
        };
    }
}
