//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Core.Configuration;
using NosCore.Core.Services.IdService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Map;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharNewPacketHandlerTests
    {
        private TestWorld World;
        private CharNewPacketHandler CharNewPacketHandler;
        private ClientSession Session;
        private Mock<IMapChangeService> MapChangeService;
        private const string TestCharacterName = "TestCharacter";

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            CharNewPacketHandler =
                new CharNewPacketHandler(World.CharacterDao, World.GenerateItemProvider(), World.QuicklistEntryDao,
                    World.ItemInstanceDao, World.InventoryItemInstanceDao, new HpService(), new MpService(), World.WorldConfiguration,
                    World.CharacterSkillDao, World.ItemList, NullLoggerFactory.Instance, World.Create<IDaoTransactionScope>());
            Session = await World.CreateSessionAsync(new List<IPacketHandler> { CharNewPacketHandler });
            MapChangeService = new Mock<IMapChangeService>();
            Session.ClearPlayerEntity();
        }

        [TestMethod]
        public async Task CreatingCharacterWhenInGameShouldFail()
        {
            await new Spec("Creating character when in game should fail")
                .GivenAsync(CharacterIsInGame)
                .WhenAsync(CreatingCharacterViaPacket)
                .ThenAsync(CharacterShouldNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingCharacterShouldSucceed()
        {
            await new Spec("Creating character should succeed")
                .WhenAsync(CreatingCharacter)
                .ThenAsync(CharacterShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingCharacterWithInvalidNameShouldFail()
        {
            await new Spec("Creating character with invalid name should fail")
                .WhenAsync(CreatingCharacterWithInvalidName)
                .ThenAsync(CharacterWithInvalidNameShouldNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingCharacterWithExistingNameShouldFail()
        {
            await new Spec("Creating character with existing name should fail")
                .WhenAsync(CreatingCharacterWithExistingName)
                .Then(OnlyOneCharacterWithNameShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CreatingCharacterInNonEmptySlotShouldFail()
        {
            await new Spec("Creating character in non empty slot should fail")
                .WhenAsync(CreatingCharacterInSlot_, 1)
                .Then(OnlyOneCharacterInSlot_ShouldExist, 1)
                .ExecuteAsync();
        }

        private async Task CharacterIsInGame()
        {
            Session = await World.CreateSessionAsync(new List<IPacketHandler> { CharNewPacketHandler });
        }

        private async Task CreatingCharacterViaPacket()
        {
            await Session.HandlePacketsAsync(new[]
            {
                new CharNewPacket
                {
                    Name = TestCharacterName
                }
            });
        }

        [TestMethod]
        public async Task ACreatedCharacterHasItsSkillsAndQuickSlots()
        {
            await new Spec("A created character has its skills and quick slots")
                .WhenAsync(CreatingCharacter)
                .ThenAsync(TheCharacterHasItsSkillsAndQuickSlots)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACharacterWhoseStarterItemDoesNotExistIsNotCreated()
        {
            await new Spec("A character whose starter item does not exist is not created")
                .Given(AStarterItemDoesNotExist)
                .WhenAsync(CreatingCharacter)
                .ThenAsync(CharacterShouldNotExist)
                .And(ThePlayerIsToldTheCreationFailed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACharacterWhoseStarterItemsDoNotFitIsNotCreated()
        {
            await new Spec("A character whose starter items do not fit is not created")
                .Given(TwoStarterItemsShareAnEquipmentSlot)
                .WhenAsync(CreatingCharacter)
                .ThenAsync(CharacterShouldNotExist)
                .And(ThePlayerIsToldTheCreationFailed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ThePlayerIsToldWhenTheCreationIsNotCommitted()
        {
            await new Spec("The player is told when the creation is not committed")
                .Given(TheDatabaseRefusesToCommit)
                .WhenAsync(CreatingCharacter)
                .Then(ThePlayerIsToldTheCreationFailed)
                .ExecuteAsync();
        }

        private void TwoStarterItemsShareAnEquipmentSlot()
        {
            World.WorldConfiguration.Value.BasicEquipments["Adventurer"] = new Dictionary<StarterOrigin, List<BasicEquipment>>
            {
                [StarterOrigin.CreateAndUpgrade] =
                [
                    new BasicEquipment { VNum = 1, Amount = 1, NoscorePocketType = NoscorePocketType.Wear },
                    new BasicEquipment { VNum = 1, Amount = 1, NoscorePocketType = NoscorePocketType.Wear }
                ]
            };
        }

        private sealed class CommitRefused : DbException;

        private void TheDatabaseRefusesToCommit()
        {
            var transaction = new Mock<IDaoTransaction>();
            transaction.Setup(t => t.CommitAsync()).ThrowsAsync(new CommitRefused());
            var scope = new Mock<IDaoTransactionScope>();
            scope.Setup(s => s.Begin()).Returns(transaction.Object);
            CharNewPacketHandler =
                new CharNewPacketHandler(World.CharacterDao, World.GenerateItemProvider(), World.QuicklistEntryDao,
                    World.ItemInstanceDao, World.InventoryItemInstanceDao, new HpService(), new MpService(), World.WorldConfiguration,
                    World.CharacterSkillDao, World.ItemList, NullLoggerFactory.Instance, scope.Object);
        }

        private void AStarterItemDoesNotExist()
        {
            World.WorldConfiguration.Value.BasicEquipments["Adventurer"] = new Dictionary<StarterOrigin, List<BasicEquipment>>
            {
                [StarterOrigin.CreateAndUpgrade] = [new BasicEquipment { VNum = 31999, Amount = 1 }]
            };
        }

        private async Task TheCharacterHasItsSkillsAndQuickSlots()
        {
            var character = (await World.CharacterDao.FirstOrDefaultAsync(s => s.Name == TestCharacterName)).NotNull();
            var skills = World.CharacterSkillDao.Where(s => s.CharacterId == character.CharacterId).NotNull()
                .Select(s => s.SkillVNum).OrderBy(s => s).ToArray();
            CollectionAssert.AreEqual(new short[] { 200, 201, 209 }, skills);
            Assert.AreEqual(4, World.QuicklistEntryDao.Where(s => s.CharacterId == character.CharacterId).NotNull().Count());
        }

        private void ThePlayerIsToldTheCreationFailed()
        {
            var told = Session.LastPackets.OfType<InfoiPacket>().Single();
            Assert.AreEqual(Game18NConstString.CreationFailed, told.Message);
        }

        private async Task CreatingCharacter()
        {
            await CharNewPacketHandler.ExecuteAsync(new CharNewPacket
            {
                Name = TestCharacterName
            }, Session);
        }

        private async Task CreatingCharacterWithInvalidName()
        {
            await CharNewPacketHandler.ExecuteAsync(new CharNewPacket
            {
                Name = "Test Character"
            }, Session);
        }

        private async Task CreatingCharacterWithExistingName()
        {
            await CharNewPacketHandler.ExecuteAsync(new CharNewPacket
            {
                Name = "TestExistingCharacter"
            }, Session);
        }

        private async Task CreatingCharacterInSlot_(int value)
        {
            await CharNewPacketHandler.ExecuteAsync(new CharNewPacket
            {
                Name = TestCharacterName,
                Slot = (byte)value
            }, Session);
        }

        private async Task CharacterShouldNotExist()
        {
            Assert.IsNull(await World.CharacterDao.FirstOrDefaultAsync(s => s.Name == TestCharacterName));
        }

        private async Task CharacterShouldExist()
        {
            Assert.IsNotNull(await World.CharacterDao.FirstOrDefaultAsync(s => s.Name == TestCharacterName));
        }

        private async Task CharacterWithInvalidNameShouldNotExist()
        {
            Assert.IsNull(await World.CharacterDao.FirstOrDefaultAsync(s => s.Name == "Test Character"));
        }

        private void OnlyOneCharacterWithNameShouldExist()
        {
            Assert.IsFalse(World.CharacterDao.Where(s => s.Name == "TestExistingCharacter").NotNull().Skip(1).Any());
        }

        private void OnlyOneCharacterInSlot_ShouldExist(int value)
        {
            Assert.IsFalse(World.CharacterDao.Where(s => s.Slot == value).NotNull().Skip(1).Any());
        }
    }
}
