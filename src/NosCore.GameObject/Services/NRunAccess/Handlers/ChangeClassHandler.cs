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


using NosCore.GameObject.Handling;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.I18N;
using Serilog;
using System;

namespace NosCore.GameObject.Services.ItemBuilder.Handling
{
    public class ChangeClassHandler : IHandler<Tuple<MapNpc, NrunPacket>, Tuple<MapNpc, NrunPacket>>
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public bool Condition(Tuple<MapNpc, NrunPacket> item) => true;

        public void Execute(RequestData<Tuple<MapNpc, NrunPacket>> requestData)
        {
            _logger.Debug($"change class");
        }
    }
}
