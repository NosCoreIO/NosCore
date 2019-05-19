using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Friend
{
    public class FdelPacketHandler : PacketHandler<FdelPacket>, IWorldPacketHandler
    {
        public override void Execute(FdelPacket fdelPacket, ClientSession session)
        {
            //TODO FIx
            //session.Character.DeleteRelation(fdelPacket.CharacterId);
            //session.SendPacket(new InfoPacket
            //{
            //    Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_DELETED, session.Account.Language)
            //});
        }
    }
}
