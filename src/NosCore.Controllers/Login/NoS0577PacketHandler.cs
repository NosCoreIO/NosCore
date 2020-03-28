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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Login;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;

namespace NosCore.PacketHandlers.Login
{
    public class NoS0577PacketHandler : PacketHandler<NoS0577Packet>, ILoginPacketHandler
    {
        private readonly ILoginService _loginService;

        public NoS0577PacketHandler(ILoginService loginService)
        {
            _loginService = loginService;
        }

        public override Task Execute(NoS0577Packet packet, ClientSession clientSession)
        {
            return _loginService.Login(null, packet.Md5String, packet.ClientVersion, clientSession,
                packet.AuthToken, true);
        }
    }
}