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

using JetBrains.Annotations;
using NosCore.GameObject.Networking;
using ChickenAPI.Packets.ClientPackets;
using ChickenAPI.Packets.ClientPackets.Player;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.ClientPackets.Families;

namespace NosCore.Controllers
{
    public class UselessPacketController : PacketController
    {
        public void CClose([UsedImplicitly] CClosePacket cClosePacket)
        {
            // idk
        }

        public void FStashEnd([UsedImplicitly] FStashEndPacket fStashEndPacket)
        {
            // idk
        }

        public void Lbs(LbsPacket lbsPacket)
        {
            // idk
        }

        public void Snap(SnapPacket snapPacket)
        {
            // pictures
        }
    }
}