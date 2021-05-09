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
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;
using NosCore.Core.MessageQueue;
using NosCore.GameObject.Messages;

namespace NosCore.PacketHandlers.Command
{
    public class KickPacketHandler : PacketHandler<KickPacket>, IWorldPacketHandler
    {
        private readonly IPubSubHub _connectedAccountHttpClient;

        public KickPacketHandler(IPubSubHub connectedAccountHttpClient)
        {
            _connectedAccountHttpClient = connectedAccountHttpClient;
        }

        public override async Task ExecuteAsync(KickPacket kickPacket, ClientSession session)
        {
            var result = await _connectedAccountHttpClient.SendMessageAsync(new KickMessage(kickPacket.Name ?? session.Character.Name)); 
            if (!result)
            {
                await session.SendPacketAsync(new InfoPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        session.Account.Language)
                }).ConfigureAwait(false);
            }
        }
    }
}