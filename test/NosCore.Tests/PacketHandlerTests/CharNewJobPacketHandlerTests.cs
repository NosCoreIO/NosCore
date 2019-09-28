using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class CharNewJobPacketHandlerTests
    {
        private Character _chara;

        private CharNewJobPacketHandler _charNewJobPacketHandler;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _chara = _session.Character;
            _session.SetCharacter(null);
            _charNewJobPacketHandler = new CharNewJobPacketHandler(TestHelpers.Instance.CharacterDao);
        }

        [TestMethod]
        public void CreateMartialArtistWhenNoLevel80_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            _charNewJobPacketHandler.Execute(new CharNewJobPacket
            {
                Name = name
            }, _session);
            Assert.IsNull(TestHelpers.Instance.CharacterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateMartialArtist_Works()
        {
            const string name = "TestCharacter";
            _chara.Level = 80;
            CharacterDto character = _chara;
            TestHelpers.Instance.CharacterDao.InsertOrUpdate(ref character);
            _charNewJobPacketHandler.Execute(new CharNewJobPacket
            {
                Name = name
            }, _session);
            Assert.IsNotNull(TestHelpers.Instance.CharacterDao.FirstOrDefault(s => s.Name == name));
        }

        [TestMethod]
        public void CreateMartialArtistWhenAlreadyOne_Does_Not_Create_Character()
        {
            const string name = "TestCharacter";
            _chara.Class = CharacterClassType.MartialArtist;
            CharacterDto character = _chara;
            _chara.Level = 80;
            TestHelpers.Instance.CharacterDao.InsertOrUpdate(ref character);
            _charNewJobPacketHandler.Execute(new CharNewJobPacket
            {
                Name = name
            }, _session);
            Assert.IsNull(TestHelpers.Instance.CharacterDao.FirstOrDefault(s => s.Name == name));
        }
    }
}