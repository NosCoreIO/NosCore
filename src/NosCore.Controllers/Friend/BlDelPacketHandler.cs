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

using System.Linq;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking.ClientSession;

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