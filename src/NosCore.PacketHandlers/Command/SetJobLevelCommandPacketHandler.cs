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

using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;
using NosCore.Core.MessageQueue;
using NosCore.Core.MessageQueue.Messages;
using Character = NosCore.Data.WebApi.Character;
using System.Linq;

namespace NosCore.PacketHandlers.Command
{
    public class SetJobLevelCommandPacketHandler(IPubSubHub pubSubHub)
        : PacketHandler<SetJobLevelCommandPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetJobLevelCommandPacket levelPacket, ClientSession session)
        {
            if (string.IsNullOrEmpty(levelPacket.Name) || (levelPacket.Name == session.Character.Name))
            {
                await session.Character.SetJobLevelAsync(levelPacket.Level).ConfigureAwait(false);
                return;
            }

            var data = new StatData
            {
                ActionType = UpdateStatActionType.UpdateJobLevel,
                Character = new Character { Name = levelPacket.Name },
                Data = levelPacket.Level
            };

            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Name == levelPacket.Name);

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