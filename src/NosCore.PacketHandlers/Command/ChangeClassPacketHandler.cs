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
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using Character = NosCore.Data.WebApi.Character;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.Command
{
    public class ChangeClassPacketHandler(IPubSubHub pubSubHub)
        : PacketHandler<ChangeClassPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ChangeClassPacket changeClassPacket, ClientSession session)
        {
            var characterName = session.Player.Name;
            if ((changeClassPacket.Name == characterName) || string.IsNullOrEmpty(changeClassPacket.Name))
            {
                session.Player.SetClass(changeClassPacket.ClassType);
                return;
            }

            var data = new StatData
            {
                ActionType = UpdateStatActionType.UpdateClass,
                Character = new Character { Name = changeClassPacket.Name },
                Data = (byte)changeClassPacket.ClassType
            };

            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Name == changeClassPacket.Name);

            if (receiver == null)
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                }).ConfigureAwait(false);
                return;
            }

            await pubSubHub.SendMessageAsync(data).ConfigureAwait(false);
        }
    }
}