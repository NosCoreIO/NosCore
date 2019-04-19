using ChickenAPI.Packets.ClientPackets.Movement;
using ChickenAPI.Packets.Enumerations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Movement
{
    public class SitPacketHandler : PacketHandler<SitPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public SitPacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override void Execute(SitPacket sitpacket, ClientSession clientSession)
        {
            sitpacket.Users.ForEach(u =>
            {
                IAliveEntity entity;

                switch (u.VisualType)
                {
                    case VisualType.Player:
                        entity = Broadcaster.Instance.GetCharacter(s => s.VisualId == u.VisualId);
                        break;
                    default:
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                            u.VisualType);
                        return;
                }

                entity.Rest();
            });
        }
    }
}
