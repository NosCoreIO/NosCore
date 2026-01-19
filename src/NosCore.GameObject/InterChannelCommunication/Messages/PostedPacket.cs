//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using System;

namespace NosCore.GameObject.InterChannelCommunication.Messages
{
    public class PostedPacket : IMessage
    {
        public string? Packet { get; set; }

        public Data.WebApi.Character? SenderCharacter { get; set; }

        public Data.WebApi.Character? ReceiverCharacter { get; set; }

        public long OriginWorldId { get; set; }

        public ReceiverType ReceiverType { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
