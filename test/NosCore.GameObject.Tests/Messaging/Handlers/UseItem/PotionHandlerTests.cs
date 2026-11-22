//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.PotionService;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Ecs.Interfaces;
using Moq;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.Interfaces;
using NosCore.Networking.SessionGroup;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Data.Enumerations.Character;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class PotionHandlerTests
    {
        private TestWorld World;
        private const short PotionVNum = 1002;
        private ClientSession _session;
        private PotionHandler _handler;
        private InventoryItemInstance _potion;
        private readonly List<MapTypeMapDto> _mapTypeMaps = new();
        private readonly List<MapTypeDto> _mapTypes = new();
        private Mock<IEquipmentStatsService> _gear;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MaxHp = 1000;
            _session.Character.MaxMp = 800;
            _gear = World.Freeze<Mock<IEquipmentStatsService>>();
            _gear.Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            _handler = new PotionHandler(new PotionCooldownService(_mapTypeMaps, _mapTypes, World.Clock),
                World.Create<RecoveryBonusService>());
        }

        [TestMethod]
        public async Task AHealthPotionRestoresItsHpAndIsConsumed()
        {
            await new Spec("A health potion adds its Hp and is used up")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 800)
                .And(MpShouldBe_, 200)
                .And(PotionCountShouldBe_, 0)
                .And(TheStatBarShouldShow_Hp_Mp, 800, 200)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheHpAPotionHealsFloatsOverTheCharacter()
        {
            await new Spec("The map is shown how much HP a potion healed")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .WhenAsync(DrinkingThePotion)
                .Then(TheMapIsShownAHealOf_, 300)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheNumberShownIsOnlyTheHpThatWasMissing()
        {
            await new Spec("A potion stronger than the missing HP shows only what it filled")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 879, 200)
                .WhenAsync(DrinkingThePotion)
                .Then(TheMapIsShownAHealOf_, 121)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AManaPotionShowsNoNumber()
        {
            await new Spec("A potion that only restores MP floats no number")
                .Given(APotionRestoring_Hp_Mp, 0, 300)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .WhenAsync(DrinkingThePotion)
                .Then(MpShouldBe_, 500)
                .And(NoHealIsShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GearThatBoostsRecoveryItemsMakesThePotionHealMore()
        {
            await new Spec("Gear raising recovery items by 20% makes a 300 Hp potion heal 360")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterWearsGearBoostingRecoveryBy_Percent, 20)
                .And(CharacterHas_Hp_Mp, 100, 200)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 460)
                .ExecuteAsync();
        }

        private void CharacterWearsGearBoostingRecoveryBy_Percent(int percent) =>
            _gear.Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None with
            {
                BCards = [new BCardDto { Type = BCardType.CardType.LeonaPassiveSkill, SubType = 21, FirstData = (short)percent }],
            });

        [TestMethod]
        public async Task ARecoveryPotionRestoresHpAndMp()
        {
            await new Spec("A recovery potion adds both its Hp and its Mp")
                .Given(APotionRestoring_Hp_Mp, 300, 300)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 400)
                .And(MpShouldBe_, 400)
                .And(PotionCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HealingStopsAtTheMaximum()
        {
            await new Spec("A potion never lifts HP or MP past the character's maximum")
                .Given(APotionRestoring_Hp_Mp, 1000, 1000)
                .And(CharacterHas_Hp_Mp, 900, 790)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 1000)
                .And(MpShouldBe_, 800)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APotionThatWouldRestoreNothingIsKept()
        {
            await new Spec("A health potion at full HP is not used up, even with MP missing")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 1000, 200)
                .WhenAsync(DrinkingThePotion)
                .Then(PotionCountShouldBe_, 1)
                .And(NoStatBarShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadCharacterCannotDrink()
        {
            await new Spec("A dead character's potion is not used and heals nothing")
                .Given(APotionRestoring_Hp_Mp, 300, 300)
                .And(CharacterHas_Hp_Mp, 0, 0)
                .And(TheCharacterIsDead)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 0)
                .And(PotionCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APotionWithoutFlatValuesIsLeftAlone()
        {
            await new Spec("A potion carrying no Hp or Mp of its own is not handled here")
                .Given(APotionRestoring_Hp_Mp, 0, 0)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .Then(PotionCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FoodIsNotAPotion()
        {
            await new Spec("Food carries Hp and Mp too, but heals over time elsewhere")
                .Given(A_Restoring_Hp_Mp, ItemType.Food, 300, 300)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 100)
                .And(PotionCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMooncakeRestoresLikeAPotion()
        {
            await new Spec("An event food with Hp and Mp heals at once and is used up")
                .Given(A_Restoring_Hp_Mp, ItemType.Event, 300, 300)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 400)
                .And(MpShouldBe_, 400)
                .And(PotionCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEventItemWithoutHpOrMpIsNotDrunk()
        {
            await new Spec("An event item that restores nothing is left to its own handler")
                .Given(A_Restoring_Hp_Mp, ItemType.Event, 0, 0)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .Then(PotionCountShouldBe_, 1)
                .And(NoStatBarShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASecondPotionWithinTheDelayIsKept()
        {
            await new Spec("A potion drunk before the map's potion delay is over heals nothing and is kept")
                .Given(_PotionsRestoring_Hp, 2, 100)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .AndAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 200)
                .And(PotionCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PotionsDrunkAtTheAllowedRateAllWork()
        {
            await new Spec("Potions drunk once the map's potion delay is over each heal")
                .Given(_PotionsRestoring_Hp, 3, 100)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .And(_MillisecondsPass, 300)
                .AndAsync(DrinkingThePotion)
                .And(_MillisecondsPass, 300)
                .AndAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 400)
                .And(PotionCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMapTypeWithALongerDelayHoldsTheNextPotionBack()
        {
            await new Spec("On a map whose type sets a 5 s potion delay, a potion one second later is kept and one after the delay heals")
                .Given(TheCharactersMapHasAPotionDelayOf_Ms, (short)5000)
                .And(_PotionsRestoring_Hp, 3, 100)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .WhenAsync(DrinkingThePotion)
                .And(_MillisecondsPass, 1000)
                .AndAsync(DrinkingThePotion)
                .And(_MillisecondsPass, 4000)
                .AndAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 300)
                .And(PotionCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullHealthPotionFillsTheHpBar()
        {
            await new Spec("A potion that restores all HP fills the bar whatever its Hp says")
                .Given(AFullRestorePotion_Hp_Mp, true, false)
                .And(CharacterHas_Hp_Mp, 10, 20)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 1000)
                .And(MpShouldBe_, 20)
                .And(PotionCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullRecoveryPotionFillsBothBars()
        {
            await new Spec("A potion that restores all HP and MP fills both bars")
                .Given(AFullRestorePotion_Hp_Mp, true, true)
                .And(CharacterHas_Hp_Mp, 10, 20)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 1000)
                .And(MpShouldBe_, 800)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullRestorePotionIsRefusedOnAPvpMap()
        {
            await new Spec("On a PvP map a full restore potion is refused and kept")
                .Given(AFullRestorePotion_Hp_Mp, true, true)
                .And(CharacterHas_Hp_Mp, 10, 20)
                .And(TheMapIsPvp)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 10)
                .And(PotionCountShouldBe_, 1)
                .And(ThePlayerIsToldItCannotBeUsedInPvp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullRestorePotionIsRefusedInAct4()
        {
            await new Spec("In Act 4 a full restore potion is refused and kept")
                .Given(AFullRestorePotion_Hp_Mp, false, true)
                .And(CharacterHas_Hp_Mp, 10, 20)
                .And(TheMapIsInAct4)
                .WhenAsync(DrinkingThePotion)
                .Then(MpShouldBe_, 20)
                .And(PotionCountShouldBe_, 1)
                .And(ThePlayerIsToldItCannotBeUsedInPvp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnOrdinaryPotionStillWorksInAct4()
        {
            await new Spec("A potion with a flat Hp still heals in Act 4")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 100, 100)
                .And(TheMapIsInAct4)
                .WhenAsync(DrinkingThePotion)
                .Then(HpShouldBe_, 400)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnOrdinaryPotionHealsTheTeamToo()
        {
            await new Spec("A 300 Hp potion also heals each team mate still standing by 300")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .AndAsync(ATeam_WithHp_OutOf_, MateType.Pet, 400, 1000)
                .AndAsync(ATeam_WithHp_OutOf_, MateType.Partner, 400, 1000)
                .WhenAsync(DrinkingThePotion)
                .Then(Every_ShouldHaveHp_, MateType.Pet, 700)
                .And(Every_ShouldHaveHp_, MateType.Partner, 700)
                .And(TheMapIsShownAHealOf_OverEveryMate, 300)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheTeamsNumbersComeBetweenTheDrinkersAndTheBars()
        {
            await new Spec("The drinker's number comes first, then the pet's, then the bars, the bag and the pet's bar")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .AndAsync(ATeam_WithHp_OutOf_, MateType.Pet, 400, 1000)
                .And(TheDrinkerSeesWhatTheMapIsShown)
                .WhenAsync(DrinkingThePotion)
                .Then(TheDrinkerIsSentTheHealsThenTheBarsThenTheBag)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMateIsOnlyHealedWhatItIsMissing()
        {
            await new Spec("A pet missing 50 HP is healed 50 by a 300 Hp potion")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .AndAsync(ATeam_WithHp_OutOf_, MateType.Pet, 950, 1000)
                .WhenAsync(DrinkingThePotion)
                .Then(Every_ShouldHaveHp_, MateType.Pet, 1000)
                .And(TheMapIsShownAHealOf_OverEveryMate, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMateAtFullHealthShowsNoNumber()
        {
            await new Spec("A pet with nothing missing floats no number")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .AndAsync(ATeam_WithHp_OutOf_, MateType.Pet, 1000, 1000)
                .WhenAsync(DrinkingThePotion)
                .Then(NoMateHealIsShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AManaPotionLeavesTheTeamAlone()
        {
            await new Spec("A potion with no Hp heals no team mate")
                .Given(APotionRestoring_Hp_Mp, 0, 300)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .AndAsync(ATeam_WithHp_OutOf_, MateType.Pet, 400, 1000)
                .WhenAsync(DrinkingThePotion)
                .Then(Every_ShouldHaveHp_, MateType.Pet, 400)
                .And(NoMateHealIsShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMateOutsideTheTeamIsNotHealed()
        {
            await new Spec("A pet out on the map but not in the team keeps its HP")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .AndAsync(ATeam_WithHp_OutOf_, MateType.Pet, 400, 1000)
                .And(ThePetLeavesTheTeam)
                .WhenAsync(DrinkingThePotion)
                .Then(Every_ShouldHaveHp_, MateType.Pet, 400)
                .ExecuteAsync();
        }

        private async Task ATeam_WithHp_OutOf_(MateType type, int hp, int maxHp)
        {
            var mate = type == MateType.Pet
                ? TestMates.Pet(_session.Character.CharacterId)
                : TestMates.Partner(_session.Character.CharacterId);
            mate.Hp = hp;
            mate.MaxHp = maxHp;
            _session.Character.Mates[mate.MateTransportId] = mate;
            await mate.SpawnAsync(_session.Character, _session.Character.MapInstance,
                _session.Character.PositionX, _session.Character.PositionY);
        }

        private void ThePetLeavesTheTeam()
        {
            foreach (var mate in _session.Character.Mates.Values.Where(m => m.MateType == MateType.Pet))
            {
                mate.IsTeamMember = false;
            }
        }

        private void Every_ShouldHaveHp_(MateType type, int hp)
        {
            foreach (var mate in _session.Character.Mates.Values.Where(m => m.MateType == type))
            {
                Assert.AreEqual(hp, mate.CurrentHp());
            }
        }

        private void TheMapIsShownAHealOf_OverEveryMate(int amount)
        {
            var shown = _session.Character.MapInstance.LastPackets.OfType<RcPacket>()
                .Where(rc => rc.VisualType == VisualType.Npc).ToList();
            Assert.AreEqual(_session.Character.Mates.Count, shown.Count);
            foreach (var mate in _session.Character.Mates.Values)
            {
                PacketAssert.AreEqual(new RcPacket
                {
                    VisualType = VisualType.Npc,
                    VisualId = mate.MateTransportId,
                    HealAmount = amount,
                    Unknown = 0
                }, shown.Single(rc => rc.VisualId == mate.MateTransportId));
            }
        }

        private void NoMateHealIsShown() =>
            Assert.IsFalse(_session.Character.MapInstance.LastPackets.OfType<RcPacket>().Any(rc => rc.VisualType == VisualType.Npc));

        // The drinker is on the map, so what the map is shown reaches them in the same stream.
        private void TheDrinkerSeesWhatTheMapIsShown()
        {
            var map = new Mock<ISessionGroup>();
            map.Setup(s => s.Broadcast(It.IsAny<IPacket[]>()))
                .Callback<IPacket[]>(Receive).Returns(Task.CompletedTask);
            map.Setup(s => s.Broadcast(It.IsAny<IPacket[]>(), It.IsAny<ISessionMatcher>()))
                .Callback<IPacket[], ISessionMatcher>((packets, _) => Receive(packets)).Returns(Task.CompletedTask);
            _session.Character.MapInstance.Sessions = map.Object;

            void Receive(IPacket[] packets)
            {
                foreach (var packet in packets)
                {
                    _session.LastPackets.Enqueue(packet);
                }
            }
        }

        private void TheDrinkerIsSentTheHealsThenTheBarsThenTheBag()
        {
            var pet = _session.Character.Mates.Values.Single();
            var received = _session.LastPackets
                .Where(p => p is RcPacket or StatPacket or IvnPacket or PstPacket).ToList();
            Assert.AreEqual(5, received.Count);
            PacketAssert.AreEqual(new RcPacket
            {
                VisualType = VisualType.Player, VisualId = _session.Character.VisualId, HealAmount = 300, Unknown = 0
            }, received[0] as RcPacket);
            PacketAssert.AreEqual(new RcPacket
            {
                VisualType = VisualType.Npc, VisualId = pet.MateTransportId, HealAmount = 300, Unknown = 0
            }, received[1] as RcPacket);
            PacketAssert.AreEqual(_session.Character.GenerateStat(), received[2] as StatPacket);
            Assert.IsInstanceOfType<IvnPacket>(received[3]);
            PacketAssert.AreEqual(pet.GeneratePst(), received[4] as PstPacket);
        }

        private void AFullRestorePotion_Hp_Mp(bool hp, bool mp)
        {
            APotionRestoring_Hp_Mp(0, 0);
            _potion.ItemInstance.Item.RestoresAllHp = hp;
            _potion.ItemInstance.Item.RestoresAllMp = mp;
        }

        private void TheMapIsPvp() => _session.Character.MapInstance.IsPvp = true;

        private void TheMapIsInAct4() => _session.Character.MapInstance.IsAct4 = true;

        private void ThePlayerIsToldItCannotBeUsedInPvp() =>
            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.CanNotUseInPvp },
                _session.LastPackets.OfType<MsgiPacket>().Single());

        private void TheCharactersMapHasAPotionDelayOf_Ms(short delay)
        {
            _mapTypes.Add(new MapTypeDto { MapTypeId = MapTypeType.Act52, PotionDelay = delay });
            _mapTypeMaps.Add(new MapTypeMapDto { MapId = _session.Character.MapInstance.Map.MapId, MapTypeId = MapTypeType.Act52 });
        }

        private void _MillisecondsPass(int ms) => World.Clock.Advance(Duration.FromMilliseconds(ms));

        [TestMethod]
        public async Task TheLastPotionOfAStackLeavesItsSlotEmpty()
        {
            await new Spec("Drinking the last potion of a stack shows its slot empty, not the potion with none left")
                .Given(APotionRestoring_Hp_Mp, 300, 0)
                .And(CharacterHas_Hp_Mp, 500, 200)
                .WhenAsync(DrinkingThePotion)
                .Then(ThePotionsSlotShouldBeShownEmpty)
                .ExecuteAsync();
        }

        private void ThePotionsSlotShouldBeShownEmpty()
        {
            var shown = _session.LastPackets.OfType<IvnPacket>().SelectMany(p => p.IvnSubPackets ?? []).OfType<IvnSubPacket>()
                .Last(i => i.Slot == 0);
            Assert.AreNotEqual(PotionVNum, shown.VNum);
        }

        private void _PotionsRestoring_Hp(int amount, int hp)
        {
            A_Restoring_Hp_Mp(ItemType.Potion, hp, 0);
            _potion.ItemInstance.Amount = (short)amount;
        }

        private void APotionRestoring_Hp_Mp(int hp, int mp) => A_Restoring_Hp_Mp(ItemType.Potion, hp, mp);

        private void A_Restoring_Hp_Mp(ItemType itemType, int hp, int mp)
        {
            var item = new Item
            {
                VNum = PotionVNum,
                Type = NoscorePocketType.Main,
                ItemType = itemType,
                Hp = (short)hp,
                Mp = (short)mp,
            };
            var instance = new ItemInstanceForTest(PotionVNum) { Amount = 1, Item = item };
            _potion = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _potion.Slot = 0;
            _potion.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_potion.ItemInstanceId] = _potion;
        }

        private void CharacterHas_Hp_Mp(int hp, int mp)
        {
            _session.Character.Hp = hp;
            _session.Character.Mp = mp;
        }

        private void TheCharacterIsDead() => _session.Character.IsAlive = false;

        private async Task DrinkingThePotion()
        {
            _session.LastPackets.Clear();
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _potion, packet));
        }

        private void HpShouldBe_(int expected) => Assert.AreEqual(expected, _session.Character.Hp);

        private void MpShouldBe_(int expected) => Assert.AreEqual(expected, _session.Character.Mp);

        private void PotionCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(PotionVNum));

        private void TheStatBarShouldShow_Hp_Mp(int hp, int mp)
        {
            var stat = _session.LastPackets.OfType<StatPacket>().LastOrDefault();
            Assert.IsNotNull(stat);
            Assert.AreEqual(hp, stat.Hp);
            Assert.AreEqual(mp, stat.Mp);
        }

        private void TheMapIsShownAHealOf_(int amount) =>
            PacketAssert.AreEqual(new RcPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.VisualId,
                HealAmount = amount,
                Unknown = 0
            }, _session.Character.MapInstance.LastPackets.OfType<RcPacket>().Single());

        private void NoHealIsShown() => Assert.IsFalse(_session.Character.MapInstance.LastPackets.OfType<RcPacket>().Any());

        private void NoStatBarShouldBeSent() => Assert.IsFalse(_session.LastPackets.OfType<StatPacket>().Any());

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
