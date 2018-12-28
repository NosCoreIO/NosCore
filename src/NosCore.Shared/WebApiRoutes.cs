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

using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Shared
{
    public static class WebApiRoutes
    {
        public static readonly string PacketRoute = "api/packet";

        public static readonly string ChannelRoute = "api/channel";

        public static readonly string RelationRoute = "api/relation";

        public static readonly string StatRoute = "api/stat";

        public static readonly string TokenRoute = "api/token";

        public static readonly string ConnectedAccountRoute = "api/connectedAccount";

        public static readonly string SessionRoute = "api/session";
    }
}
