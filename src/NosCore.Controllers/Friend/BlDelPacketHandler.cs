using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using System.Linq;

namespace NosCore.PacketHandlers.Friend
{
    public class BlDelPacketHandler : PacketHandler<BlDelPacket>, IWorldPacketHandler
    {
        private readonly IBlacklistHttpClient _blacklistHttpClient;

        public BlDelPacketHandler(IBlacklistHttpClient blacklistHttpClient)
        {
            _blacklistHttpClient = blacklistHttpClient;
        }

        public override void Execute(BlDelPacket bldelPacket, ClientSession session)
        {
            var list = _blacklistHttpClient.GetBlackLists(session.Character.VisualId);
            var idtorem = list.FirstOrDefault(s => s.CharacterId == bldelPacket.CharacterId);
            if (idtorem != null)
            {
                _blacklistHttpClient.DeleteFromBlacklist(idtorem.CharacterRelationId);
                session.SendPacket(session.Character.GenerateBlinit(_blacklistHttpClient));
            }
            else
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_IN_BLACKLIST,
                            session.Account.Language)
                });
            }
        }
    }
}
