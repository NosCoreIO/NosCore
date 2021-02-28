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
