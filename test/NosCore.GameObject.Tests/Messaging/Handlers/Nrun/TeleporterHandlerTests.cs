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
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class TeleporterHandlerTests
    {
        private const int TeleporterNpcId = 100;
        private const int OtherNpcId = 101;

        private ClientSession _session = null!;
        private Mock<IMapChangeService> _mapChangeService = null!;
        private List<TeleporterDto> _teleporters = null!;
        private INrunEventHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _mapChangeService = new Mock<IMapChangeService>();
            _teleporters = new List<TeleporterDto>
            {
                new() { MapNpcId = TeleporterNpcId, Runner = NrunRunnerType.Teleport, Index = 0, MapId = 1, MapX = 81, MapY = 114 },
                new() { MapNpcId = TeleporterNpcId, Runner = NrunRunnerType.Teleport, Index = 2, MapId = 145, MapX = 13, MapY = 114 },
                new() { MapNpcId = OtherNpcId, Runner = NrunRunnerType.Teleport, Index = 1, MapId = 20, MapX = 8, MapY = 87 },
            };
            _session.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto>
                {
                    new() { MapNpcId = TeleporterNpcId, MapId = _session.Character.MapInstance.Map.MapId, VNum = 1 },
                    new() { MapNpcId = OtherNpcId, MapId = _session.Character.MapInstance.Map.MapId, VNum = 1 },
                },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1 } });
            _handler = new TeleporterHandler(_mapChangeService.Object, _teleporters);
        }

        [TestMethod]
        public async Task RunnersMatchTheirMenus()
        {
            await new Spec("Each teleport menu answers its own n_run")
                .Then(HandlerOf_ShouldAnswer_, typeof(TeleporterHandler), NrunRunnerType.Teleport)
                .And(HandlerOf_ShouldAnswer_, typeof(ActVTeleporterHandler), NrunRunnerType.TeleportActV)
                .And(HandlerOf_ShouldAnswer_, typeof(WeddingGazeboTeleporterHandler), NrunRunnerType.GoToWeddingGazebo)
                .And(HandlerOf_ShouldAnswer_, typeof(GlacerusCaveTeleporterHandler), NrunRunnerType.GoToGlacerusCave)
                .And(HandlerOf_ShouldAnswer_, typeof(ForgottenArchipelagoTeleporterHandler), NrunRunnerType.GoToForgottenArchipelago)
                .And(HandlerOf_ShouldAnswer_, typeof(GlacernonReturnHandler), NrunRunnerType.ReturnFromGlacernon)
                .And(HandlerOf_ShouldAnswer_, typeof(MortazDesertReturnHandler), NrunRunnerType.ReturnFromMortazDesert)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NullTargetIsIgnored()
        {
            await new Spec("A null target is a no-op")
                .Given(CharacterGoldIs_, 10_000L)
                .WhenAsync(OptionIsPickedWithoutTarget_, (short)2)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NonNpcTargetIsIgnored()
        {
            await new Spec("A target that is not an NPC is a no-op")
                .Given(CharacterGoldIs_, 10_000L)
                .WhenAsync(OptionIsPickedOnAPlayer_, (short)2)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OptionTakesThePlayerToItsRecordedDestination()
        {
            await new Spec("The second option costs 2 000 gold and lands on the destination recorded for it")
                .Given(CharacterGoldIs_, 10_000L)
                .WhenAsync(Option_IsPickedAtNpc_, (short)2, TeleporterNpcId)
                .Then(CharacterGoldShouldBe_, 8_000L)
                .And(PlayerShouldBeSentTo_At_, (short)145, (short)13, (short)114)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FirstOptionIsFree()
        {
            await new Spec("Option 0 of the town teleporter costs nothing")
                .Given(CharacterGoldIs_, 0L)
                .WhenAsync(Option_IsPickedAtNpc_, (short)0, TeleporterNpcId)
                .Then(CharacterGoldShouldBe_, 0L)
                .And(PlayerShouldBeSentTo_At_, (short)1, (short)81, (short)114)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NotEnoughGoldKeepsThePlayerHere()
        {
            await new Spec("Without the fare the player is told so and stays")
                .Given(CharacterGoldIs_, 1_999L)
                .WhenAsync(Option_IsPickedAtNpc_, (short)2, TeleporterNpcId)
                .Then(CharacterGoldShouldBe_, 1_999L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .And(PlayerShouldBeToldNotEnoughGold)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherNpcsDestinationIsNotOffered()
        {
            await new Spec("An option recorded only for another NPC does nothing here")
                .Given(CharacterGoldIs_, 10_000L)
                .WhenAsync(Option_IsPickedAtNpc_, (short)1, TeleporterNpcId)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ActVTeleportCostsFiveThousandPerOption()
        {
            await new Spec("The Act V teleporters charge 5 000 gold per option index")
                .Given(CharacterGoldIs_, 20_000L)
                .And(TheMenuIsTheActVTeleporter)
                .WhenAsync(Option_IsPickedAtNpc_, (short)2, TeleporterNpcId)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(PlayerShouldBeSentTo_At_, (short)145, (short)13, (short)114)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WeddingGazeboCostsFiveHundred()
        {
            await new Spec("The wedding gazebo ferry takes 500 gold")
                .Given(CharacterGoldIs_, 1_000L)
                .And(TheMenuIsTheWeddingGazeboFerry)
                .WhenAsync(Option_IsPickedAtNpc_, (short)0, TeleporterNpcId)
                .Then(CharacterGoldShouldBe_, 500L)
                .And(PlayerShouldBeSentTo_At_, (short)1, (short)81, (short)114)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GlacerusCaveIsFree()
        {
            await new Spec("The Glacerus cave guide takes no gold")
                .Given(CharacterGoldIs_, 0L)
                .And(TheMenuIsTheGlacerusCaveGuide)
                .WhenAsync(Option_IsPickedAtNpc_, (short)2, TeleporterNpcId)
                .Then(CharacterGoldShouldBe_, 0L)
                .And(PlayerShouldBeSentTo_At_, (short)145, (short)13, (short)114)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADestinationOfAnotherMenuIsNotOffered()
        {
            await new Spec("A destination recorded under the town teleporter's menu is not reached through a free menu")
                .Given(CharacterGoldIs_, 10_000L)
                .And(TheMenuSentIsTheGlacerusCaveGuide)
                .WhenAsync(Option_IsPickedAtNpc_, (short)2, TeleporterNpcId)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADestinationWithoutARecordedMenuIsNotOffered()
        {
            await new Spec("A destination stored before its menu was recorded takes nobody anywhere")
                .Given(CharacterGoldIs_, 10_000L)
                .And(TheDestinationsOfNpc_HaveNoRecordedMenu, TeleporterNpcId)
                .WhenAsync(Option_IsPickedAtNpc_, (short)2, TeleporterNpcId)
                .Then(CharacterGoldShouldBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GlacernonReturnIsFreeAndUsesTheNpcDestination()
        {
            _handler = new GlacernonReturnHandler(_mapChangeService.Object, _teleporters);
            TheDestinationsOfNpc_BelongTo_(TeleporterNpcId, _handler.Runner);
            CharacterGoldIs_(0);
            await Option_IsPickedAtNpc_(0, TeleporterNpcId);
            CharacterGoldShouldBe_(0);
            PlayerShouldBeSentTo_At_(1, 81, 114);
        }

        [TestMethod]
        public async Task GlacernonReturnDoesNotUseAnotherNpcOrMenuDestination()
        {
            _handler = new GlacernonReturnHandler(_mapChangeService.Object, _teleporters);
            TheDestinationsOfNpc_BelongTo_(OtherNpcId, _handler.Runner);
            await Option_IsPickedAtNpc_(1, TeleporterNpcId);
            await Option_IsPickedAtNpc_(0, TeleporterNpcId);
            NoMapChangeShouldHaveBeenInvoked();
        }

        private void TheMenuSentIsTheGlacerusCaveGuide() =>
            _handler = new GlacerusCaveTeleporterHandler(_mapChangeService.Object, _teleporters);

        private void TheDestinationsOfNpc_HaveNoRecordedMenu(int npcId) =>
            _teleporters.Where(s => s.MapNpcId == npcId).ToList().ForEach(s => s.Runner = null);

        private void TheDestinationsOfNpc_BelongTo_(int npcId, NrunRunnerType runner) =>
            _teleporters.Where(s => s.MapNpcId == npcId).ToList().ForEach(s => s.Runner = runner);

        private void HandlerOf_ShouldAnswer_(System.Type type, NrunRunnerType runner)
        {
            var handler = (INrunEventHandler)System.Activator.CreateInstance(type, _mapChangeService.Object, _teleporters)!;
            Assert.AreEqual(runner, handler.Runner);
        }

        private void TheMenuIsTheActVTeleporter()
        {
            _handler = new ActVTeleporterHandler(_mapChangeService.Object, _teleporters);
            TheDestinationsOfNpc_BelongTo_(TeleporterNpcId, _handler.Runner);
        }

        private void TheMenuIsTheWeddingGazeboFerry()
        {
            _handler = new WeddingGazeboTeleporterHandler(_mapChangeService.Object, _teleporters);
            TheDestinationsOfNpc_BelongTo_(TeleporterNpcId, _handler.Runner);
        }

        private void TheMenuIsTheGlacerusCaveGuide()
        {
            _handler = new GlacerusCaveTeleporterHandler(_mapChangeService.Object, _teleporters);
            TheDestinationsOfNpc_BelongTo_(TeleporterNpcId, _handler.Runner);
        }

        private void CharacterGoldIs_(long gold) => _session.Character.Gold = gold;

        private Task Option_IsPickedAtNpc_(short index, int npcId) =>
            _handler.HandleAsync(_session, _session.Character.MapInstance.Npcs.First(s => s.VisualId == npcId),
                new NrunPacket { Runner = _handler.Runner, Type = index, VisualType = VisualType.Npc, VisualId = npcId });

        private Task OptionIsPickedWithoutTarget_(short index) =>
            _handler.HandleAsync(_session, null,
                new NrunPacket { Runner = _handler.Runner, Type = index, VisualType = VisualType.Npc, VisualId = TeleporterNpcId });

        private Task OptionIsPickedOnAPlayer_(short index) =>
            _handler.HandleAsync(_session, new Mock<IAliveEntity>().Object,
                new NrunPacket { Runner = _handler.Runner, Type = index, VisualType = VisualType.Npc, VisualId = TeleporterNpcId });

        private void CharacterGoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void PlayerShouldBeSentTo_At_(short mapId, short x, short y) =>
            _mapChangeService.Verify(s => s.ChangeMapAsync(_session, mapId, x, y), Times.Once);

        private void NoMapChangeShouldHaveBeenInvoked() =>
            _mapChangeService.Verify(s => s.ChangeMapAsync(
                It.IsAny<ClientSession>(), It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);

        private void PlayerShouldBeToldNotEnoughGold() =>
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.NotEnoughGold));
    }
}
