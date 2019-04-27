using System;
using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ClientPackets.Drops;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class CharacterDeletePacketHandlerTests
    {
        private ClientSession _session;
        private CharacterDeletePacketHandler _characterDeletePacketHandler;
        [TestInitialize]
        public void Setup()
        {
            new Mapper();
            _session = TestHelpers.Instance.GenerateSession();
            _characterDeletePacketHandler = new CharacterDeletePacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.AccountDao);
        }

        [TestMethod]
        public void DeleteCharacter_Invalid_Password()
        {
            _session.Character.MapInstance = null;
            _characterDeletePacketHandler.Execute(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "testpassword"
            }, _session);
            Assert.IsNotNull(
                TestHelpers.Instance.CharacterDao
                    .FirstOrDefault(s => s.AccountId == _session.Account.AccountId && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacterWhenInGame_Does_Not_Delete_Character()
        {
            _characterDeletePacketHandler.Execute(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, _session);
            Assert.IsNotNull(
                TestHelpers.Instance.CharacterDao
                    .FirstOrDefault(s => s.AccountId == _session.Account.AccountId && s.State == CharacterState.Active));
        }

        [TestMethod]
        public void DeleteCharacter()
        {

            _session.Character.MapInstance = null;
            _characterDeletePacketHandler.Execute(new CharacterDeletePacket
            {
                Slot = 1,
                Password = "test"
            }, _session);
            Assert.IsNull(
                TestHelpers.Instance.CharacterDao
                    .FirstOrDefault(s => s.AccountId == _session.Account.AccountId && s.State == CharacterState.Active));
        }
    }
}