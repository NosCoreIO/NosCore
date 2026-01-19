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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;
using TwoFactorAuthNet;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Services.GuriRunnerService.Handlers
{
    public class MfaGuriHandler : IGuriEventHandler
    {
        public bool Condition(GuriPacket packet)
        {
            return (packet.Type == GuriPacketType.TextInput && packet.Argument == 3 && packet.VisualId == 0);
        }

        public async Task ExecuteAsync(RequestData<GuriPacket> requestData)
        {
            if (requestData.ClientSession.MfaValidated != false || requestData.ClientSession.Account.MfaSecret == null)
            {
                return;
            }

            var tfa = new TwoFactorAuth();
            if (tfa.VerifyCode(requestData.ClientSession.Account.MfaSecret, requestData.Data.Value))
            {
                requestData.ClientSession.MfaValidated = true;
                await requestData.ClientSession.HandlePacketsAsync(new[] { new EntryPointPacket()
                {
                    Name = requestData.ClientSession.Account.Name
                } });
            }
            else
            {
                await requestData.ClientSession.SendPacketAsync(new NosCore.Packets.ServerPackets.UI.GuriPacket
                {
                    Type = GuriPacketType.TextInput,
                    Argument = 3,
                    EntityId = 0
                });

                await requestData.ClientSession.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.IncorrectPassword });
            }
        }
    }
}