using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Core.MessageQueue;

namespace NosCore.GameObject.Messages
{
    public class UpdateGoldStatsMessage : IMessage
    {
        public UpdateGoldStatsMessage(string body)
        {
            Body = body;
            Id = Guid.NewGuid();
        }
        
        public Guid Id { get; set; }
        public string Body { get; set; }
        public DateTime VisibilityTimeout { get; set; }
    }
}
