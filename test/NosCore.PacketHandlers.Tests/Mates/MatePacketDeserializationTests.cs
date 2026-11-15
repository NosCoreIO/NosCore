//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking;
using NosCore.PacketHandlers.Mates;
using NosCore.Packets;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.Mates
{
    // say_p is also a server header, so the line has to come out as the client packet the handler is closed over.
    [TestClass]
    public class MatePacketDeserializationTests
    {
        private static readonly Deserializer Deserializer = new(PacketTypes.World());

        private static readonly Serializer Serializer = new(PacketTypes.World());

        private static IPacket RoundTrip(IPacket packet) => Deserializer.Deserialize(Serializer.Serialize(packet));

        [TestMethod]
        public void PartnerControlReachesItsHandler()
        {
            var packet = new PtctlPacket
            {
                MapType = 1,
                Amount = 2,
                Mates =
                [
                    new PtctlSubPacket { VisualId = 1135623, PositionX = 50, PositionY = 132 },
                    new PtctlSubPacket { VisualId = 1135626, PositionX = 49, PositionY = 133 },
                ],
                VisualId = 1135623,
                Speed = 9,
            };

            Assert.IsInstanceOfType(RoundTrip(packet), HandledBy(typeof(PtctlPacketHandler)));
        }

        [TestMethod]
        public void APetsSpeechReachesItsHandler()
        {
            Assert.IsInstanceOfType(RoundTrip(new SayPPacket { PetId = 1135623, Message = "good boy" }), HandledBy(typeof(SayPPacketHandler)));
        }

        [TestMethod]
        public void APartnerSkillReachesItsHandler()
        {
            var packet = new SuCtlPacket { CastId = 200, MateTransportId = 1135623, TargetType = VisualType.Monster, TargetId = 2045 };

            Assert.IsInstanceOfType(RoundTrip(packet), HandledBy(typeof(SuCtlPacketHandler)));
        }

        [TestMethod]
        public void APetSkillReachesItsHandler()
        {
            var packet = new UpetPacket { MateTransportId = 1135623, TargetType = VisualType.Monster, TargetId = 2045, SkillVNum = 664 };

            Assert.IsInstanceOfType(RoundTrip(packet), HandledBy(typeof(UpetPacketHandler)));
        }

        [TestMethod]
        public void TheSkillOfAPetOrderIsReadWithOrWithoutTheCellItIsSentWith()
        {
            var bare = (UpetPacket)RoundTrip(new UpetPacket { MateTransportId = 1135623, TargetType = VisualType.Monster, TargetId = 2045, SkillVNum = 664 });
            var withCell = (UpetPacket)RoundTrip(new UpetPacket
            {
                MateTransportId = 1135623, TargetType = VisualType.Monster, TargetId = 2045, SkillVNum = 664, MapX = 144, MapY = 124
            });

            Assert.AreEqual((1135623L, VisualType.Monster, 2045L, (short)664, (short?)null, (short?)null),
                (bare.MateTransportId, bare.TargetType, bare.TargetId, bare.SkillVNum, bare.MapX, bare.MapY));
            Assert.AreEqual((1135623L, VisualType.Monster, 2045L, (short)664, (short?)144, (short?)124),
                (withCell.MateTransportId, withCell.TargetType, withCell.TargetId, withCell.SkillVNum, withCell.MapX, withCell.MapY));
        }

        [TestMethod]
        public void APetsWholeSentenceIsRead()
        {
            var packet = (SayPPacket)RoundTrip(new SayPPacket { PetId = 1135623, Message = "good boy, sit" });

            Assert.AreEqual((1135623L, "good boy, sit"), (packet.PetId, packet.Message));
        }

        private static Type HandledBy(Type handler) => handler.BaseType.NotNull().GetGenericArguments()[0];
    }
}
