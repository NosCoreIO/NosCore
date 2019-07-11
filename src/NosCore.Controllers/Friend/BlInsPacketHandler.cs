using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Friend
{
    public class BlInsPackettHandler : PacketHandler<BlInsPacket>, IWorldPacketHandler
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IWebApiAccess _webApiAccess;
        public BlInsPackettHandler(IWebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public override void Execute(BlInsPacket blinsPacket, ClientSession session)
        {
            var result = _webApiAccess.Post<LanguageKey>(WebApiRoute.Blacklist, new BlacklistRequest { CharacterId = session.Character.CharacterId, BlInsPacket = blinsPacket });
            switch (result)
            {
                case LanguageKey.CANT_BLOCK_FRIEND:
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_BLOCK_FRIEND,
                            session.Account.Language)
                    });
                    break;
                case LanguageKey.ALREADY_BLACKLISTED:
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_BLACKLISTED,
                            session.Account.Language)
                    });
                    break;
                case LanguageKey.BLACKLIST_ADDED:
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_ADDED,
                            session.Account.Language)
                    });
                    session.SendPacket(session.Character.GenerateBlinit(_webApiAccess));
                    break;
                default:
                    _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.FRIEND_REQUEST_DISCONNECTED));
                    break;
            }
        }
    }
}
