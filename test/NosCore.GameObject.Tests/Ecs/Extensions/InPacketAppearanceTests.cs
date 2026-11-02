//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class InPacketAppearanceTests
    {
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        [TestMethod]
        public void ATransformedPlayerShowsTheSpecialistAndItsWings()
        {
            _session.Character.UseSp = true;
            _session.Character.Morph = 3;
            _session.Character.MorphUpgrade = 7;
            _session.Character.MorphDesign = 2;

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!;

            Assert.AreEqual((byte)3, sub.Morph);
            Assert.AreEqual((byte)7, sub.MorphUpgrade);
            Assert.AreEqual((byte)2, sub.MorphUpgrade2);
        }

        [TestMethod]
        public void AnUntransformedPlayerShowsNoMorph()
        {
            _session.Character.Morph = 3;
            _session.Character.MorphUpgrade = 7;
            _session.Character.MorphDesign = 2;

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!;

            Assert.AreEqual((byte)0, sub.Morph);
            Assert.AreEqual((byte)0, sub.MorphUpgrade);
            Assert.AreEqual((byte)0, sub.MorphUpgrade2);
        }

        [TestMethod]
        public void AWornFairyIsShown()
        {
            var fairy = _session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(TestHelpers.Instance.GenerateItemProvider().Create(2, 1), _session.Character.CharacterId))!
                .First();
            fairy.Type = NoscorePocketType.Wear;
            fairy.Slot = (byte)EquipmentType.Fairy;

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!;

            Assert.AreEqual((byte)4, sub.Fairy);
            Assert.AreEqual((byte)fairy.ItemInstance.Item.Element, sub.FairyElement);
            Assert.AreEqual((byte)fairy.ItemInstance.Item.Morph, sub.FairyMorph);
        }
    }
}
