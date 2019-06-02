using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Friend
{
    public class FlPacketHandler : PacketHandler<FlPacket>, IWorldPacketHandler
    {
        public override void Execute(FlPacket flPacket, ClientSession session)
        {
            var target =
                Broadcaster.Instance.GetCharacter(s => s.Name == flPacket.CharacterName);

            if (target == null)
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        session.Account.Language)
                });
                return;
            }

            var fins = new FinsPacket
            {
                CharacterId = target.VisualId,
                Type = FinsPacketType.Accepted
            };

            session.HandlePackets(new[] { fins });
        }
    }
}
