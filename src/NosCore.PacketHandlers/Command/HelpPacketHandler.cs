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

using NosCore.Core.Extensions;
using NosCore.Data.CommandPackets;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class HelpPacketHandler : PacketHandler<HelpPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(HelpPacket helpPacket, ClientSession session)
        {
            await session.SendPacketAsync(session.Character.GenerateSay("-------------Help command-------------",
                SayColorType.Purple)).ConfigureAwait(false);
            var classes = helpPacket.GetType().Assembly.GetTypes().Where(t =>
                    typeof(ICommandPacket).IsAssignableFrom(t)
                    && (t.GetCustomAttribute<CommandPacketHeaderAttribute>()?.Authority <= session.Account.Authority))
                .OrderBy(x => x.Name).ToList();
            foreach (var type in classes)
            {
                var classInstance = type.CreateInstance<ICommandPacket>();
                var method = type.GetMethod("Help");
                if (method == null)
                {
                    continue;
                }

                var message = method.Invoke(classInstance, null)?.ToString();
                if (!string.IsNullOrEmpty(message))
                {
                    await session.SendPacketAsync(session.Character.GenerateSay(message, SayColorType.Green)).ConfigureAwait(false);
                }
            }
        }
    }
}