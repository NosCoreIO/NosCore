using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NosCore.Core.MessageQueue;

namespace NosCore.GameObject.Messages
{
    public class UpdateGoldMessage : IMessage
    {
        public UpdateGoldMessage(string characterName, long gold)
        {
            Body = JsonSerializer.Serialize(new
            {
                CharacterName = characterName,
                Gold = gold,
            });
            Id = Guid.NewGuid();
        }
        
        public Guid Id { get; set; }
        public string Body { get; set; }
        public DateTime VisibilityTimeout { get; set; }
    }
}
