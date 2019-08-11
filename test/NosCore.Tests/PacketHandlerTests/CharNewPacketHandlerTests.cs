using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ClientPackets.Drops;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapInstanceProvider.Handlers;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class CharNewPacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private ClientSession _session;
        private Character _chara;
        private CharNewPacketHandler _charNewPacketHandler;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _chara = _session.Character;
            _session.SetCharacter(null);
            _charNewPacketHandler = new CharNewPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.MinilandDao);
        }

        [TestMethod]
        public void CreateCharacterWhenInGame_Does_Not_Create_Character()
        {
            _session.SetCharacter(_chara);
            _session.Character.MapInstance =
                new MapInstance(new Map(), new Guid(), true, MapInstanceType.BaseMapInstance,
                    new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    _logger, new List<IMapInstanceEventHandler>());
            const string name = "TestCharacter";
            _charNewPacketHandler.Execute(new CharNewPacket
            {
                Name = name
            }, _session);
            Assert.IsNull(TestHelpers.Instance.CharacterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateCharacter()
        {
            const string name = "TestCharacter";
            _charNewPacketHandler.Execute(new CharNewPacket
            {
                Name = name
            }, _session);
            Assert.IsNotNull(TestHelpers.Instance.CharacterDao.FirstOrDefault(s => s.Name == name));
        }


        [TestMethod]
        public void InvalidName_Does_Not_Create_Character()
        {
            const string name = "Test Character";
            _charNewPacketHandler.Execute(new CharNewPacket
            {
                Name = name,
            }, _session);
            Assert.IsNull(TestHelpers.Instance.CharacterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void ExistingName_Does_Not_Create_Character()
        {
            const string name = "TestExistingCharacter";
            _charNewPacketHandler.Execute(new CharNewPacket
            {
                Name = name,
            }, _session);
            Assert.IsFalse(TestHelpers.Instance.CharacterDao.Where(s => s.Name == name).Skip(1).Any());
        }

        [TestMethod]
        public void NotEmptySlot_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            _charNewPacketHandler.Execute(new CharNewPacket
            {
                Name = name,
                Slot = 1
            }, _session);
            Assert.IsFalse(TestHelpers.Instance.CharacterDao.Where(s => s.Slot == 1).Skip(1).Any());
        }
    }
}