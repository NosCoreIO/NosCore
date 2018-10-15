//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Diagnostics.CodeAnalysis;

namespace NosCore.Shared.Enumerations.Map
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum PortalType : sbyte
    {
        MapPortal = -1,
        TsNormal = 0, // same over >127 - sbyte
        Closed = 1,
        Open = 2,
        Miniland = 3,
        TsEnd = 4,
        TsEndClosed = 5,
        Exit = 6,
        ExitClosed = 7,
        Raid = 8,
        Effect = 9, // same as 13 - 19 and 20 - 126
        BlueRaid = 10,
        DarkRaid = 11,
        TimeSpace = 12,
        ShopTeleport = 20
    }
}