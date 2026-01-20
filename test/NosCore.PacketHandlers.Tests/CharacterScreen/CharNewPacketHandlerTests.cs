//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Core.Services.IdService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class CharNewPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Character Chara = null!;
        private CharNewPacketHandler CharNewPacketHandler = null!;
        private ClientSession Session = null!;
        private Mock<IMapChangeService> MapChangeService = null!;
        private const string TestCharacterName = "TestCharacter";

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            CharNewPacketHandler =
                new CharNewPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.MinilandDao, new Mock<IItemGenerationService>().Object, new Mock<IDao<QuicklistEntryDto, Guid>>().Object,
                    new Mock<IDao<IItemInstanceDto?, Guid>>().Object, new Mock<IDao<InventoryItemInstanceDto, Guid>>().Object, new HpService(), new MpService(), TestHelpers.Instance.WorldConfiguration, new Mock<IDao<CharacterSkillDto, Guid>>().Object);
            Session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler> { CharNewPacketHandler });
            Chara = Session.Character;
            MapChangeService = new Mock<IMapChangeService>();
            TypeAdapterConfig<CharacterDto, Character>.NewConfig().ConstructUsing(src => Chara);
            await Session.SetCharacterAsync(null);
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
            var idServer = new IdService<MapItem>(1);
            await Session.SetCharacterAsync(Chara);
            Session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance,
                    new MapItemGenerationService(new EventLoaderService<MapItem, Tuple<MapItem, GetPacket>, IGetMapItemEventHandler>(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()), idServer),
                    Logger, TestHelpers.Instance.Clock, MapChangeService.Object, new Mock<ISessionGroupFactory>().Object, TestHelpers.Instance.SessionRegistry, TestHelpers.Instance.DistanceCalculator);
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
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == TestCharacterName));
        }

        private async Task CharacterShouldExist()
        {
            Assert.IsNotNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == TestCharacterName));
        }

        private async Task CharacterWithInvalidNameShouldNotExist()
        {
            Assert.IsNull(await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(s => s.Name == "Test Character"));
        }

        private void OnlyOneCharacterWithNameShouldExist()
        {
            Assert.IsFalse(TestHelpers.Instance.CharacterDao.Where(s => s.Name == "TestExistingCharacter")!.Skip(1).Any());
        }

        private void OnlyOneCharacterInSlot_ShouldExist(int value)
        {
            Assert.IsFalse(TestHelpers.Instance.CharacterDao.Where(s => s.Slot == value)!.Skip(1).Any());
        }
    }
}
