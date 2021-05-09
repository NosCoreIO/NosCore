using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NosCore.Core.MessageQueue;

namespace NosCore.GameObject.Messages
{
    public class KickMessage : IMessage
    {
        public KickMessage(string characterName)
        {
            Body = JsonSerializer.Serialize(new
            {
                CharacterName = characterName
            });
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }
        public string Body { get; set; }
    }
}
