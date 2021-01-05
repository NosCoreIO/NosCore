using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Core.HubInterfaces;

namespace NosCore.GameObject.HubClients.ChannelHubClient.Events
{
    public class KickEvent : IEvent
    {
        public KickEvent(long characterId)
        {
            CharacterId = characterId;
        }
        public long CharacterId { get; set; }
    }
}
