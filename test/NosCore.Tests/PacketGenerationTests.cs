using log4net;
using log4net.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Serializing;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.I18N;
using System.IO;
using System.Reflection;

namespace NosCore.Tests
{
    [TestClass]
    public class PacketGenerationTests
    {
        private const string ConfigurationPath = "../../../configuration";

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(ConfigurationPath + "/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(PacketGenerationTests)));
        }

        [TestMethod]
        public void GenerateInPacketIsNotCorruptedForCharacter()
        {
            var characterTest = new Character() { Name = "characterTest" };

            var packet = PacketFactory.Serialize(characterTest.GenerateIn());
            Assert.AreEqual("in 1 characterTest - 0 0 0 0 0 0 0 0 0 -1.-1.-1.-1.-1.-1.-1.-1.-1 0 0 0 -1 0 0 0 0 0 0 0 0 0 - 0 0 0 0 0 0 0 0 0 0 0", packet);
        }

        [TestMethod]
        public void GenerateInPacketIsNotCorruptedForMonster()
        {
            var mapMonsterTest = new MapMonster();

            var packet = PacketFactory.Serialize(mapMonsterTest.GenerateIn());
            Assert.AreEqual("in 3 - 0 0 0 0 0 0 0 0 0 -1 0 0 -1 - 0 -1 0 0 0 0 0 0 0 0", packet);
        }

        [TestMethod]
        public void GenerateInPacketIsNotCorruptedForNpc()
        {
            var mapNpcTest = new MapNpc();

            var packet = PacketFactory.Serialize(mapNpcTest.GenerateIn());
            Assert.AreEqual("in 2 - 0 0 0 0 0 0 0 0 0 -1 0 0 -1 - 0 -1 0 0 0 0 0 0 0 0", packet);
        }

        [TestMethod]
        public void GenerateInPacketIsNotCorruptedForItem()
        {
            var mapItemTest = new MapItem();

            var packet = PacketFactory.Serialize(mapItemTest.GenerateIn());
            Assert.AreEqual("in 9 - 0 0 0 0 0 0 0", packet);
        }
    }
}