﻿using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.AspNetCore.Mvc;
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
    public class FdelPacketHandler : PacketHandler<FdelPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly IWebApiAccess _webApiAccess;
        public FdelPacketHandler(ILogger logger, IWebApiAccess webApiAccess)
        {
            _logger = logger;
            _webApiAccess = webApiAccess;
        }

        public override void Execute(FdelPacket fdelPacket, ClientSession session)
        {
            var list = _webApiAccess.Get<List<CharacterRelationStatus>>(WebApiRoute.Friend, 
                session.Character.VisualId);
            var idtorem = list.FirstOrDefault(s => s.CharacterId == fdelPacket.CharacterId);
            if (idtorem != null)
            {
                _webApiAccess.Delete<IActionResult>(WebApiRoute.Friend, idtorem.CharacterRelationId);
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_DELETED, session.Account.Language)
                });
                var targetCharacter = Broadcaster.Instance.GetCharacter(s => s.VisualId == fdelPacket.CharacterId);
                if ( targetCharacter != null)
                {
                    targetCharacter.SendPacket(targetCharacter.GenerateFinit(_webApiAccess));
                }

                session.Character.SendPacket(session.Character.GenerateFinit(_webApiAccess));
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
