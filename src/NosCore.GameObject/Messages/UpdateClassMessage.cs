using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NosCore.Core.MessageQueue;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messages
{
    public class UpdateClassMessage : IMessage
    {
        public UpdateClassMessage(string characterName, CharacterClassType classType)
        {
            Body = JsonSerializer.Serialize(new
            {
                CharacterName = characterName,
                ClassType = classType,
            });
            Id = Guid.NewGuid();
        }
        
        public Guid Id { get; set; }
        public string Body { get; set; }
    }
}
