using System;

namespace NosCore.Core.MessageQueue.Messages
{
    public class DisconnectData : IMessage
    {
        public long CharacterId { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
