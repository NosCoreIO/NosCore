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
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;
using Mate = NosCore.GameObject.Services.MateService.Mate;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class PetNameHandlerTests
    {
        private const short TagVNum = 2157;

        private TestWorld World;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(1);
        }

        [TestMethod]
        public async Task TheTypedNameRenamesThePetAndUsesUpTheTagAsync()
        {
            var pet = Owned(TestMates.Pet(_session.Character.CharacterId));
            HoldingTags(2);

            await TypingAsync(pet.MateTransportId, "Kiwi");

            Assert.AreEqual("Kiwi", pet.Name);
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(TagVNum));
            Assert.AreEqual(Game18NConstString.NewNameAssignedToCompanion,
                _session.LastPackets.OfType<MsgiPacket>().Last().Message);
        }

        [TestMethod]
        public async Task WithoutATagThePetKeepsItsNameAsync()
        {
            var pet = Owned(TestMates.Pet(_session.Character.CharacterId));

            await TypingAsync(pet.MateTransportId, "Kiwi");

            Assert.AreNotEqual("Kiwi", pet.Name);
        }

        [TestMethod]
        public async Task APartnerCannotBeRenamedAsync()
        {
            var partner = Owned(TestMates.Partner(_session.Character.CharacterId));
            HoldingTags(1);

            await TypingAsync(partner.MateTransportId, "Kiwi");

            Assert.AreNotEqual("Kiwi", partner.Name);
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(TagVNum));
        }

        [TestMethod]
        public async Task AnEmptyNameKeepsTheTagAsync()
        {
            var pet = Owned(TestMates.Pet(_session.Character.CharacterId));
            HoldingTags(1);

            await TypingAsync(pet.MateTransportId, " ");

            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(TagVNum));
        }

        private Mate Owned(Mate mate)
        {
            _session.Character.Mates[mate.MateTransportId] = mate;
            return mate;
        }

        private void HoldingTags(short amount)
        {
            var item = new Item
            {
                VNum = TagVNum,
                Type = NoscorePocketType.Etc,
                ItemType = ItemType.Teacher,
                Effect = (ItemEffectType)TeacherItemEffectType.PetNameTag
            };
            var tag = InventoryItemInstance.Create(new ItemInstanceForTest(TagVNum) { Amount = amount, Item = item },
                _session.Character.CharacterId);
            tag.Slot = 8;
            tag.Type = NoscorePocketType.Etc;
            _session.Character.InventoryService[tag.ItemInstanceId] = tag;
        }

        private Task TypingAsync(long transportId, string name) =>
            World.Create<PetNameHandler>().Handle(new GuriPacketReceivedEvent(_session, new ClientGuriPacket
            {
                Type = GuriPacketType.TextInputReply,
                Argument = (int)PetNameHandler.TextInputArgument,
                VisualId = 1,
                Data = transportId,
                Value = name
            }));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Etc };
            public object Clone() => MemberwiseClone();
        }
    }
}
