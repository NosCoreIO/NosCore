using ChickenAPI.Packets.ClientPackets.Relations;

namespace NosCore.Data.WebApi
{
    public class FriendShipRequest
    {
        public long CharacterId { get; set; }
        public FinsPacket FinsPacket { get; set; }
    }
}