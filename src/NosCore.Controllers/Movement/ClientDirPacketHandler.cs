using ChickenAPI.Packets.ClientPackets.Movement;
using ChickenAPI.Packets.Enumerations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Movement
{
    public class ClientDirPacketHandler : PacketHandler<ClientDirPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public ClientDirPacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override void Execute(ClientDirPacket dirpacket, ClientSession session)
        {
            IAliveEntity entity;
            switch (dirpacket.VisualType)
            {
                case VisualType.Player:
                    entity = session.Character;
                    break;
                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                        dirpacket.VisualType);
                    return;
            }

            entity.ChangeDir(dirpacket.Direction);
        }
    }
}