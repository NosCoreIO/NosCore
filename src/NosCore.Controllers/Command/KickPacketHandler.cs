//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Command
{
    public class KickPacketHandler : PacketHandler<KickPacket>, IWorldPacketHandler
    {
        public override void Execute(KickPacket kickPacket, ClientSession session)
        {
            var servers = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                .Where(s => s.Type == ServerType.WorldServer);
            ServerConfiguration config = null;
            ConnectedAccount account = null;

            foreach (var server in servers)
            {
                config = server.WebApi;
                account = WebApiAccess.Instance.Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, config)
                    .Find(s => s.ConnectedCharacter.Name == kickPacket.Name);
                if (account != null)
                {
                    break;
                }
            }

            if (account == null) //TODO: Handle 404 in WebApi
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        session.Account.Language)
                });
                return;
            }

            WebApiAccess.Instance.Delete<ConnectedAccount>(WebApiRoute.Session, config, account.ConnectedCharacter.Id);
        }
    }
}