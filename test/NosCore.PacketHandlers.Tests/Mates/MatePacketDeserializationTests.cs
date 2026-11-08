//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.PacketHandlers.Mates;
using NosCore.Packets;
using NosCore.Packets.Attributes;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;

namespace NosCore.PacketHandlers.Tests.Mates
{
    // say_p is also a server header, so the line has to come out as the client packet the handler is closed over.
    [TestClass]
    public class MatePacketDeserializationTests
    {
        private static readonly Deserializer Deserializer = new(typeof(IPacket).Assembly.GetTypes()
            .Where(p => p.GetInterfaces().Contains(typeof(IPacket))
                && (p.GetCustomAttribute<PacketHeaderAttribute>() == null
                    || (p.GetCustomAttribute<PacketHeaderAttribute>()!.Scopes & Scope.OnLoginScreen) == 0)
                && p.IsClass && !p.IsAbstract));

        [TestMethod]
        [DataRow("ptctl 1 2 1135623 50 132 1135626 49 133 1135623 9", typeof(PtctlPacketHandler))]
        [DataRow("say_p 1135623 good boy", typeof(SayPPacketHandler))]
        [DataRow("suctl 200 0 1135623 3 2045", typeof(SuCtlPacketHandler))]
        public void TheClientsLineReachesItsHandler(string line, Type handler)
        {
            Assert.IsInstanceOfType(Deserializer.Deserialize(line), handler.BaseType!.GetGenericArguments()[0]);
        }

        [TestMethod]
        public void APetsWholeSentenceIsRead()
        {
            var packet = (SayPPacket)Deserializer.Deserialize("say_p 1135623 good boy, sit");

            Assert.AreEqual((1135623L, "good boy, sit"), (packet.PetId, packet.Message));
        }
    }
}
