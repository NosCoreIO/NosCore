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
using System.Threading.Tasks;
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

        public override async Task Execute(BlDelPacket bldelPacket, ClientSession session)
        {
            var list = await _blacklistHttpClient.GetBlackLists(session.Character.VisualId).ConfigureAwait(false);
            var idtorem = list.FirstOrDefault(s => s.CharacterId == bldelPacket.CharacterId);
            if (idtorem != null)
            {
                await _blacklistHttpClient.DeleteFromBlacklist(idtorem.CharacterRelationId).ConfigureAwait(false);
                await session.SendPacket(await session.Character.GenerateBlinit(_blacklistHttpClient).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                await session.SendPacket(new InfoPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_IN_BLACKLIST,
                        session.Account.Language)
                }).ConfigureAwait(false);
            }
        }
    }
}