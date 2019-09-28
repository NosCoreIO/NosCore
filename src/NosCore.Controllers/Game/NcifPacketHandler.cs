using ChickenAPI.Packets.ClientPackets.Battle;
using ChickenAPI.Packets.Enumerations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Game
{
    public class NcifPacketHandler : PacketHandler<NcifPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public NcifPacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override void Execute(NcifPacket ncifPacket, ClientSession session)
        {
            IAliveEntity entity;

            switch (ncifPacket.Type)
            {
                case VisualType.Player:
                    entity = Broadcaster.Instance.GetCharacter(s => s.VisualId == ncifPacket.TargetId);
                    break;
                case VisualType.Monster:
                    entity = session.Character.MapInstance.Monsters.Find(s => s.VisualId == ncifPacket.TargetId);
                    break;
                case VisualType.Npc:
                    entity = session.Character.MapInstance.Npcs.Find(s => s.VisualId == ncifPacket.TargetId);
                    break;
                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                        ncifPacket.Type);
                    return;
            }

            session.SendPacket(entity?.GenerateStatInfo());
        }
    }
}