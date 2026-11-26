//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.Packets;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class PenaltyCommandSerializationTests
    {
        private readonly Serializer _serializer = new(new[] { typeof(MutePacket), typeof(BanPacket) });
        private readonly Deserializer _deserializer = new(new[] { typeof(MutePacket), typeof(BanPacket) });

        [TestMethod]
        public void AMuteReasonKeepsEveryWord()
        {
            var packet = (MutePacket)_deserializer.Deserialize(_serializer.Serialize(
                new MutePacket { Name = "Tester", Minutes = 30, Reason = "spam in chat" }));

            Assert.AreEqual("Tester", packet.Name);
            Assert.AreEqual(30, packet.Minutes);
            Assert.AreEqual("spam in chat", packet.Reason);
        }

        [TestMethod]
        public void ABanWithoutAReasonHasNone()
        {
            var packet = (BanPacket)_deserializer.Deserialize(_serializer.Serialize(new BanPacket { Name = "Tester", Days = 0 }));

            Assert.AreEqual(0, packet.Days);
            Assert.IsNull(packet.Reason);
        }
    }
}
