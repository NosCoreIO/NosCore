using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Friend
{
    public class BlPacketHandler : PacketHandler<BlPacket>, IWorldPacketHandler
    {
        public override void Execute(BlPacket finsPacket, ClientSession session)
        {
            var target =
                Broadcaster.Instance.GetCharacter(s => s.Name == finsPacket.CharacterName);

            if (target == null)
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        session.Account.Language)
                });
                return;
            }

            var blinsPacket = new BlInsPacket
            {
                CharacterId = target.VisualId
            };

            session.HandlePackets(new[] { blinsPacket });
        }
    }
}
