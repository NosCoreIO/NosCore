using System;

namespace NosCore.GameObject.InterChannelCommunication.Messages
{
    public class DisconnectData : IMessage
    {
        public long CharacterId { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
