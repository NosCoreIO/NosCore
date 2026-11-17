//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class InPacketAppearanceTests
    {
        private TestWorld World;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public void ATransformedPlayerShowsTheSpecialistAndItsWings()
        {
            _session.Character.UseSp = true;
            _session.Character.Morph = 3;
            _session.Character.MorphUpgrade = 7;
            _session.Character.MorphDesign = 2;

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull();

            Assert.AreEqual((short)3, sub.Morph);
            Assert.AreEqual((byte)7, sub.MorphUpgrade);
            Assert.AreEqual((byte)2, sub.MorphUpgrade2);
        }

        [TestMethod]
        public void ThePlayersShownTitleIsInTheMapEntry()
        {
            _session.Character.Titles = new List<TitleDto>
            {
                new() { TitleType = 9301, Visible = false, Active = true },
                new() { TitleType = 9300, Visible = true, Active = false },
            };

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull();

            Assert.AreEqual((short)9300, sub.Title);
        }

        [TestMethod]
        public void AnUntransformedPlayerShowsNoMorph()
        {
            _session.Character.Morph = 3;
            _session.Character.MorphUpgrade = 7;
            _session.Character.MorphDesign = 2;

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull();

            Assert.AreEqual((short)0, sub.Morph);
            Assert.AreEqual((byte)0, sub.MorphUpgrade);
            Assert.AreEqual((byte)0, sub.MorphUpgrade2);
        }

        [TestMethod]
        public void ARiderShowsTheMountToNewcomersAndInTheirOwnMapEntry()
        {
            _session.Character.IsVehicled = true;
            _session.Character.Morph = 2369;

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull();
            var cInfo = _session.Character.GenerateCInfo(World.GameLanguageLocalizer);

            Assert.AreEqual((short)2369, sub.Morph);
            Assert.AreEqual((short)2369, cInfo.Morph);
        }

        [TestMethod]
        public void TheFamilysLevelIsShownInTheMapEntryAndToNewcomers()
        {
            _session.Character.Family = new Family { FamilyId = 1, Name = "Testers", FamilyLevel = 7 };

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull();
            var cInfo = _session.Character.GenerateCInfo(World.GameLanguageLocalizer);

            Assert.AreEqual((byte)7, sub.FamilyLevel);
            Assert.AreEqual((byte)7, cInfo.FamilyLevel);
        }

        [TestMethod]
        public void TheInfoCardShowsTheSpecialistOnlyWhileTransformed()
        {
            _session.Character.Morph = 3;
            var untransformed = _session.Character.GenerateReqInfo(World.GameLanguageLocalizer, RegionType.EN);
            _session.Character.UseSp = true;
            var transformed = _session.Character.GenerateReqInfo(World.GameLanguageLocalizer, RegionType.EN);

            Assert.AreEqual(0L, untransformed.Morph);
            Assert.AreEqual(3L, transformed.Morph);
        }

        [TestMethod]
        public void AWornFairyIsShown()
        {
            var fairy = _session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(World.GenerateItemProvider().Create(2, 1), _session.Character.CharacterId)).NotNull()
                .First();
            fairy.Type = NoscorePocketType.Wear;
            fairy.Slot = (byte)EquipmentType.Fairy;

            var sub = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull();

            Assert.AreEqual((byte)4, sub.Fairy);
            Assert.AreEqual((byte)fairy.ItemInstance.Item.Element, sub.FairyElement);
            Assert.AreEqual((byte)fairy.ItemInstance.Item.Morph, sub.FairyMorph);
        }
    }
}
