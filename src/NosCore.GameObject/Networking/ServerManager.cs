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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.WebApi;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
{
    public sealed class ServerManager : BroadcastableBase
    {
        private static ServerManager _instance;


        public static ServerManager Instance => _instance ?? (_instance = new ServerManager());

        private long _lastGroupId = 1;
        public ConcurrentDictionary<long, Group> Groups { get; set; } = new ConcurrentDictionary<long, Group>();

        public long GetNextGroupId()
        {
            return ++_lastGroupId;
        }

        }
}