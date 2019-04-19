using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Friend
{
    public class BlInsPackettHandler : PacketHandler<BlInsPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        public BlInsPackettHandler( ILogger logger)
        {
            _logger = logger;
        }

        public override void Execute(BlInsPacket blinsPacket, ClientSession session)
        {
            if (Broadcaster.Instance.GetCharacter(s => s.VisualId == blinsPacket.CharacterId) == null)
            {
                _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.USER_NOT_CONNECTED,
                    session.Account.Language));
                return;
            }

            if (session.Character.CharacterRelations.Values.Any(s =>
                s.RelatedCharacterId == blinsPacket.CharacterId && s.RelationType != CharacterRelationType.Blocked))
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_BLOCK_FRIEND,
                        session.Account.Language)
                });
                return;
            }

            if (session.Character.CharacterRelations.Values.Any(s =>
                s.RelatedCharacterId == blinsPacket.CharacterId && s.RelationType == CharacterRelationType.Blocked))
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_BLACKLISTED,
                        session.Account.Language)
                });
                return;
            }

            session.Character.AddRelation(blinsPacket.CharacterId, CharacterRelationType.Blocked);
            session.SendPacket(new InfoPacket
            {
                Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_ADDED, session.Account.Language)
            });
        }
    }
}
