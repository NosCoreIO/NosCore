//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class HairStyleDyeHandlerTests
    {
        private const short DyesVNum = 2533;
        private TestWorld World;
        private ClientSession _session;
        private HairStyleDyeHandler _handler;
        private InventoryItemInstance _dyes;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Inject(new List<HairStyleDyeDto>
            {
                new() { HairStyleDyeId = 1, ItemVNum = DyesVNum, HairStyle = HairStyleType.FauxHawkUndercut, DyedHairStyle = HairStyleType.SavageFauxHawkUndercut },
                new() { HairStyleDyeId = 2, ItemVNum = DyesVNum, HairStyle = HairStyleType.ChoppyBangs, DyedHairStyle = HairStyleType.SavageChoppyBangs },
            });
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(1);
            _handler = World.Create<HairStyleDyeHandler>();
            var dyes = new Item { VNum = DyesVNum, Type = NoscorePocketType.Etc, ItemType = ItemType.Magical, Effect = ItemEffectType.HairStyleDye };
            var instance = InventoryItemInstance.Create(new ItemInstanceForTest(dyes) { Amount = 1 }, _session.Character.CharacterId);
            instance.Slot = 0;
            instance.Type = NoscorePocketType.Etc;
            _session.Character.InventoryService[instance.ItemInstanceId] = instance;
            _dyes = instance;
        }

        [TestMethod]
        public async Task AFauxHawkUndercutIsDyed()
        {
            await new Spec("The dyes turn a Faux Hawk Undercut into its dyed look and are used up")
                .Given(WearingHairStyle_, HairStyleType.FauxHawkUndercut)
                .WhenAsync(UsingTheDyes)
                .Then(TheMapShouldSeeHairStyle_, HairStyleType.SavageFauxHawkUndercut)
                .And(DyesLeftShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChoppyBangsAreDyed()
        {
            await new Spec("The dyes turn Choppy Bangs into their dyed look")
                .Given(WearingHairStyle_, HairStyleType.ChoppyBangs)
                .WhenAsync(UsingTheDyes)
                .Then(TheMapShouldSeeHairStyle_, HairStyleType.SavageChoppyBangs)
                .And(DyesLeftShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherHairStyleKeepsTheDyes()
        {
            await new Spec("Another hairstyle is refused with the client's line and keeps the dyes")
                .Given(WearingHairStyle_, HairStyleType.JellyRolls)
                .WhenAsync(UsingTheDyes)
                .Then(TheMapShouldSeeHairStyle_, HairStyleType.JellyRolls)
                .And(TheOnlyPunkAndBombMessageShouldBeSent)
                .And(DyesLeftShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAlreadyDyedHairStyleCannotBeDyedAgain()
        {
            await new Spec("A dyed hairstyle cannot be dyed again")
                .Given(WearingHairStyle_, HairStyleType.SavageFauxHawkUndercut)
                .WhenAsync(UsingTheDyes)
                .Then(TheMapShouldSeeHairStyle_, HairStyleType.SavageFauxHawkUndercut)
                .And(TheOnlyPunkAndBombMessageShouldBeSent)
                .And(DyesLeftShouldBe_, 1)
                .ExecuteAsync();
        }

        private void WearingHairStyle_(HairStyleType style) => _session.Character.HairStyle = style;

        private Task UsingTheDyes() => _handler.Handle(new ItemUsedEvent(_session, _dyes, new UseItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Etc,
            Slot = 0,
            Mode = 1,
        }));

        private void TheMapShouldSeeHairStyle_(HairStyleType style) =>
            Assert.AreEqual(style, _session.Character.GenerateEq().HairStyle);

        private void DyesLeftShouldBe_(int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(DyesVNum));

        private void TheOnlyPunkAndBombMessageShouldBeSent() =>
            Assert.AreEqual(Game18NConstString.OnlyWithPunkAndBomb,
                _session.LastPackets.OfType<MsgiPacket>().Single().Message);

        private sealed class ItemInstanceForTest(Item item) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = item.VNum;
            public Item Item { get; set; } = item;
            public object Clone() => MemberwiseClone();
        }
    }
}
