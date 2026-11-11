//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class MedalOfHonourHandlerTests
    {
        private TestWorld World;
        private const short MedalVNum = 5899;
        private ClientSession _session;
        private MedalOfHonourHandler _handler;
        private InventoryItemInstance _medal;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.GenerateSessionAsync();
            _handler = new MedalOfHonourHandler();
        }

        [TestMethod]
        public async Task TheMedalGivesItsReputationAndIsUsedUp()
        {
            await new Spec("A Medal of Honour adds its value to the reputation, updates the bar and is consumed")
                .Given(AMedal_WithEffect_Worth_, ItemType.Special, ItemEffectType.MedalOfHonour, 200)
                .And(CharacterHasReputation_, 1000L)
                .WhenAsync(UsingTheMedal)
                .Then(ReputationShouldBe_, 1200L)
                .And(MedalCountShouldBe_, 0)
                .And(TheReputationBarShouldShow_, 1200L)
                .And(ReputationIncreasedBy_ShouldBeAnnounced, 200)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHolderSharingTheEffectIsIgnored()
        {
            await new Spec("A card holder carries the same effect number but is a box, so the handler leaves it alone")
                .Given(AMedal_WithEffect_Worth_, ItemType.Box, ItemEffectType.MedalOfHonour, 1)
                .And(CharacterHasReputation_, 1000L)
                .WhenAsync(UsingTheMedal)
                .Then(ReputationShouldBe_, 1000L)
                .And(MedalCountShouldBe_, 1)
                .ExecuteAsync();
        }

        private void AMedal_WithEffect_Worth_(ItemType itemType, ItemEffectType effect, int value)
        {
            var item = new Item
            {
                VNum = MedalVNum,
                Type = NoscorePocketType.Main,
                ItemType = itemType,
                Effect = effect,
                EffectValue = value,
            };
            var inst = new ItemInstanceForTest(MedalVNum) { Amount = 1, Item = item };
            _medal = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _medal.Slot = 0;
            _medal.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_medal.ItemInstanceId] = _medal;
        }

        private void CharacterHasReputation_(long reputation) => _session.Character.Reputation = reputation;

        private async Task UsingTheMedal()
        {
            await _handler.Handle(new ItemUsedEvent(_session, _medal, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 0,
                Parameter = 0,
            }));
        }

        private void ReputationShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Reputation);

        private void MedalCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(MedalVNum));

        private void TheReputationBarShouldShow_(long reputation)
        {
            var fd = _session.LastPackets.OfType<FdPacket>().LastOrDefault();
            Assert.IsNotNull(fd);
            Assert.AreEqual(reputation, fd.Reput);
        }

        private void ReputationIncreasedBy_ShouldBeAnnounced(int amount)
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .SingleOrDefault(p => p.Message == Game18NConstString.ReputationIncreased);
            Assert.IsNotNull(say);
            Assert.AreEqual(amount, Convert.ToInt32(say.Game18NArguments.Single()));
        }

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
