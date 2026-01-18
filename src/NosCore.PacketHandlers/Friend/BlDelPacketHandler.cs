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
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;

namespace NosCore.PacketHandlers.Friend
{
    public class BlDelPacketHandler(IBlacklistHub blacklistHttpClient,
            IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<BlDelPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(BlDelPacket bldelPacket, ClientSession session)
        {
            var list = await blacklistHttpClient.GetBlacklistedAsync(session.Character.VisualId).ConfigureAwait(false);
            var idtorem = list.FirstOrDefault(s => s.CharacterId == bldelPacket.CharacterId);
            if (idtorem != null)
            {
                await blacklistHttpClient.DeleteAsync(idtorem.CharacterRelationId).ConfigureAwait(false);
                await session.SendPacketAsync(await session.Character.GenerateBlinitAsync(blacklistHttpClient).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                await session.SendPacketAsync(new InfoPacket
                {
                    Message = gameLanguageLocalizer[LanguageKey.NOT_IN_BLACKLIST,
                        session.Account.Language]
                }).ConfigureAwait(false);
            }
        }
    }
}