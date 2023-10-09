using System;

namespace NosCore.GameObject.InterChannelCommunication.Messages
{
    public class DeleteMailData : IMessage
    {
        public long CharacterId { get; set; }
        public short MailId { get; set; }
        public byte PostType { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
