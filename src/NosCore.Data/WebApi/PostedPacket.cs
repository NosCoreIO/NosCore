using NosCore.Shared.Enumerations.Interaction;

namespace NosCore.Data.WebApi
{
    public class PostedPacket
    {
        public string Packet { get; set; }

        public Character SenderCharacter { get; set; }

        public Character ReceiverCharacter { get; set; }

        public int OriginWorldId { get; set; }

        public ReceiverType ReceiverType { get; set; }
    }
}