using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Friend
{
    public class BlDelPacketHandler : PacketHandler<BlDelPacket>, IWorldPacketHandler
    {
        public override void Execute(BlDelPacket bldelPacket, ClientSession session)
        {
            //TODO Fix
            //if (!session.Character.CharacterRelations.Values.Any(s =>
            //    s.RelatedCharacterId == bldelPacket.CharacterId && s.RelationType == CharacterRelationType.Blocked))
            //{
            //    session.SendPacket(new InfoPacket
            //    {
            //        Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_IN_BLACKLIST,
            //            session.Account.Language)
            //    });
            //    return;
            //}

            //session.Character.DeleteBlackList(bldelPacket.CharacterId);
        }
    }
}
