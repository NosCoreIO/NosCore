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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Act4;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BaseCampService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BaseCampService
{
    [TestClass]
    public class BaseCampTests
    {
        private const short AngelFlagVNum = 1446;
        private const short DemonFlagVNum = 1447;
        private const short ScrollVNum = 2311;
        private const short AngelCampVNum = 965;
        private const short DemonCampVNum = 966;

        private TestWorld World;
        private ClientSession _angel;
        private ClientSession _demon;
        private MapInstance _glacernon;
        private Mock<IMapChangeService> _mapChange;
        private BaseFlagHandler _flags;
        private BaseCampTeleportHandler _scrolls;
        private BaseCampFellHandler _fell;
        private GameObject.Services.BaseCampService.BaseCampService _camps;
        private InventoryItemInstance _item;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _angel = await World.CreateSessionAsync();
            _demon = await World.CreateSessionAsync();
            _glacernon = World.GetBaseMap(TestWorld.TestMapId);
            _glacernon.IsAct4 = true;
            _glacernon.MapInstanceType = MapInstanceType.BaseMapInstance;
            _angel.Character.MapInstance = _glacernon;
            _demon.Character.MapInstance = _glacernon;
            _angel.Character.Faction = FactionType.Angel;
            _demon.Character.Faction = FactionType.Demon;
            _angel.Character.PositionX = 12;
            _angel.Character.PositionY = 14;
            World.Inject(new List<NpcMonsterDto>
            {
                new() { NpcMonsterVNum = AngelCampVNum, MaxHp = 18000, Level = 85 },
                new() { NpcMonsterVNum = DemonCampVNum, MaxHp = 18000, Level = 85 },
            });
            _camps = World.Create<GameObject.Services.BaseCampService.BaseCampService>();
            World.Inject<IBaseCampService>(_camps);
            _mapChange = World.Freeze<Mock<IMapChangeService>>();
            _flags = World.Create<BaseFlagHandler>();
            _scrolls = World.Create<BaseCampTeleportHandler>();
            _fell = World.Create<BaseCampFellHandler>();
        }

        [TestMethod]
        public async Task AFlagBuildsTheCampWhereItsUserStands()
        {
            await new Spec("An angel planting the angel flag in Glacernon builds the angel camp at their feet")
                .Given(TheAngelHasA_, AngelFlagVNum)
                .WhenAsync(TheAngelUsesIt)
                .Then(AnAngelCampShouldStandAt_, (short)12, (short)14)
                .And(TheItemShouldBeUsedUp)
                .And(TheDemonShouldBeTold_, Game18NConstString.AngelCampBuilt)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFlagOnlyWorksInGlacernon()
        {
            await new Spec("A flag used outside Glacernon builds nothing")
                .Given(TheAngelHasA_, AngelFlagVNum)
                .And(TheAngelIsOutsideGlacernon)
                .WhenAsync(TheAngelUsesIt)
                .Then(TheAngelShouldBeTold_, Game18NConstString.OnlyGlacernon2)
                .And(NoCampShouldStand)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyTheFlagsSideCanPlantIt()
        {
            await new Spec("An angel cannot plant the demon flag")
                .Given(TheAngelHasA_, DemonFlagVNum)
                .WhenAsync(TheAngelUsesIt)
                .Then(TheAngelShouldBeTold_, Game18NConstString.OnlyYouCanCampFlag)
                .And(NoCampShouldStand)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASecondFlagAsksBeforeReplacingTheCamp()
        {
            await new Spec("A side that already has a camp is asked before a second flag replaces it")
                .Given(TheAngelHasA_, AngelFlagVNum)
                .AndAsync(TheAngelUsesIt)
                .And(TheAngelHasA_, AngelFlagVNum)
                .WhenAsync(TheAngelUsesIt)
                .Then(TheAngelShouldBeAsked_, Game18NConstString.BuildCampAfterRemovingTheLast)
                .And(_CampsShouldStand, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingReplacesTheCamp()
        {
            await new Spec("Confirming the second flag tears the old camp down and builds the new one")
                .Given(TheAngelHasA_, AngelFlagVNum)
                .AndAsync(TheAngelUsesIt)
                .And(TheAngelHasA_, AngelFlagVNum)
                .And(TheAngelMovesTo_, (short)20, (short)22)
                .WhenAsync(TheAngelConfirms)
                .Then(_CampsShouldStand, 1)
                .And(AnAngelCampShouldStandAt_, (short)20, (short)22)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheScrollNeedsAFinishedCamp()
        {
            await new Spec("The teleport scroll does nothing while the side's camp is still being built")
                .Given(TheAngelHasA_, AngelFlagVNum)
                .AndAsync(TheAngelUsesIt)
                .And(TheAngelHasA_, ScrollVNum)
                .And(_MinutesPass, 29)
                .WhenAsync(TheAngelUsesIt)
                .Then(TheAngelShouldBeTold_, Game18NConstString.NoBaseCamp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheScrollTakesItsUserToTheFinishedCamp()
        {
            await new Spec("Thirty minutes after the flag, the scroll takes its user to the camp after its bar")
                .Given(TheAngelHasA_, AngelFlagVNum)
                .AndAsync(TheAngelUsesIt)
                .And(TheAngelHasA_, ScrollVNum)
                .And(TheAngelMovesTo_, (short)30, (short)31)
                .And(_MinutesPass, 30)
                .AndAsync(TheAngelUsesIt)
                .And(_SecondsPass, 5)
                .WhenAsync(TheAngelConfirms)
                .Then(TheAngelShouldHaveBeenTakenTo_, (short)12, (short)14)
                .And(TheItemShouldBeUsedUp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenCampTakesNobodyAnywhere()
        {
            await new Spec("Once its camp is torn down, the side's scroll has nowhere to go")
                .Given(TheAngelHasA_, AngelFlagVNum)
                .AndAsync(TheAngelUsesIt)
                .AndAsync(TheCampFalls)
                .And(TheAngelHasA_, ScrollVNum)
                .And(_MinutesPass, 30)
                .WhenAsync(TheAngelUsesIt)
                .Then(TheAngelShouldBeTold_, Game18NConstString.NoBaseCamp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyEnemiesCanAttackACamp()
        {
            await new Spec("Only the other side can attack a camp")
                .Given(TheAngelHasA_, AngelFlagVNum)
                .WhenAsync(TheAngelUsesIt)
                .Then(_CanAttackTheCamp, "angel", false)
                .And(_CanAttackTheCamp, "demon", true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PetsFightForTheirOwnersSide()
        {
            await new Spec("A pet cannot attack its owner's camp but can attack the other side's")
                .Given(TheAngelHasA_, AngelFlagVNum)
                .AndAsync(TheAngelUsesIt)
                .WhenAsync(BothSidesCallAPet)
                .Then(_sPetCanAttackTheCamp, "angel", false)
                .And(_sPetCanAttackTheCamp, "demon", true)
                .ExecuteAsync();
        }

        private async Task BothSidesCallAPet()
        {
            foreach (var session in new[] { _angel, _demon })
            {
                var pet = TestMates.Pet(session.Character.CharacterId);
                session.Character.Mates[pet.MateTransportId] = pet;
                await pet.SpawnAsync(session.Character, _glacernon, 5, 5);
            }
        }

        private void _sPetCanAttackTheCamp(string who, bool expected)
        {
            if ((who == "angel" ? _angel : _demon).Character.Mates.Values.Single().Entity is not { } pet)
            {
                Assert.Fail("The pet did not spawn.");
                return;
            }

            Assert.AreEqual(expected, GameObject.Services.BattleService.BattleService.CanAttack(pet, Camps().Single()));
        }

        private void TheAngelHasA_(short vnum)
        {
            var item = vnum switch
            {
                AngelFlagVNum => new Item { VNum = vnum, Type = NoscorePocketType.Main, Effect = ItemEffectType.AngelBaseFlag, EffectValue = AngelCampVNum },
                DemonFlagVNum => new Item { VNum = vnum, Type = NoscorePocketType.Main, Effect = ItemEffectType.DemonBaseFlag, EffectValue = DemonCampVNum },
                _ => new Item { VNum = vnum, Type = NoscorePocketType.Etc, Effect = ItemEffectType.Teleport, EffectValue = (short)TeleportEffectValue.BaseCamp },
            };
            var instance = new ItemInstance(item) { Amount = 1 };
            _item = InventoryItemInstance.Create(instance, _angel.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = item.Type;
            _angel.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void TheAngelIsOutsideGlacernon() => _glacernon.IsAct4 = false;

        private void TheAngelMovesTo_(short x, short y)
        {
            _angel.Character.PositionX = x;
            _angel.Character.PositionY = y;
        }

        private void _MinutesPass(int minutes) => World.Clock.AdvanceMinutes(minutes);

        private void _SecondsPass(int seconds) => World.Clock.AdvanceSeconds(seconds);

        private Task TheAngelUsesIt() => Use(0);

        private Task TheAngelConfirms() => Use(1);

        private async Task Use(byte mode)
        {
            _angel.LastPackets.Clear();
            _demon.LastPackets.Clear();
            var evt = new ItemUsedEvent(_angel, _item, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _angel.Character.CharacterId,
                Type = _item.Type.ToPocketType(),
                Slot = 0,
                Mode = mode,
                Parameter = 0,
            });
            await _flags.Handle(evt);
            await _scrolls.Handle(evt);
        }

        private IEnumerable<MonsterComponentBundle> Camps() => _glacernon.EnumerateMonsters()
            .Where(m => m.IsAlive && m.VNum is AngelCampVNum or DemonCampVNum);

        private async Task TheCampFalls()
        {
            var camp = Camps().Single();
            camp.IsAlive = false;
            await _fell.Handle(new EntityDiedEvent(camp, _demon.Character));
        }

        private void AnAngelCampShouldStandAt_(short x, short y)
        {
            var camp = Camps().Single();
            Assert.AreEqual(AngelCampVNum, camp.VNum);
            Assert.AreEqual(FactionType.Angel, camp.Faction);
            Assert.AreEqual((x, y), (camp.PositionX, camp.PositionY));
        }

        private void NoCampShouldStand() => Assert.IsFalse(Camps().Any());

        private void _CampsShouldStand(int count) => Assert.AreEqual(count, Camps().Count());

        private void TheItemShouldBeUsedUp() =>
            Assert.IsNull(_angel.Character.InventoryService.LoadBySlotAndType(0, _item.Type));

        private void TheAngelShouldBeTold_(Game18NConstString message) =>
            Assert.IsTrue(_angel.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == message));

        private void TheDemonShouldBeTold_(Game18NConstString message) =>
            Assert.IsTrue(_demon.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == message));

        private void TheAngelShouldBeAsked_(Game18NConstString question) =>
            Assert.AreEqual(question, _angel.LastPackets.OfType<QnaiPacket>().Single().Question);

        private void TheAngelShouldHaveBeenTakenTo_(short x, short y) =>
            _mapChange.Verify(m => m.ChangeMapInstanceAsync(_angel, _glacernon.MapInstanceId, x, y), Times.Once);

        private void _CanAttackTheCamp(string who, bool expected) =>
            Assert.AreEqual(expected, GameObject.Services.BattleService.BattleService.CanAttack(
                (who == "angel" ? _angel : _demon).Character, Camps().Single()));
    }
}
