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

using System;
using System.Collections.Generic;
using System.Reflection;
using NosCore.Packets.Attributes;

namespace NosCore.GameObject.Services.PacketHandlerService
{
    public class PacketHandlerRegistry : IPacketHandlerRegistry
    {
        private readonly Dictionary<Type, PacketHeaderAttribute> _attributeDic = new();
        private readonly Dictionary<Type, IPacketHandler> _handlersByPacketType = new();

        public PacketHandlerRegistry(IEnumerable<IPacketHandler> packetsHandlers)
        {
            foreach (var handler in packetsHandlers)
            {
                var type = handler.GetType().BaseType?.GenericTypeArguments[0];
                if (type == null)
                {
                    continue;
                }

                if (!_attributeDic.ContainsKey(type))
                {
                    var attr = type.GetCustomAttribute<PacketHeaderAttribute>(true);
                    if (attr != null)
                    {
                        _attributeDic.Add(type, attr);
                    }
                }

                if (!_handlersByPacketType.ContainsKey(type))
                {
                    _handlersByPacketType.Add(type, handler);
                }
            }
        }

        public IPacketHandler? GetHandler(Type packetType)
        {
            _handlersByPacketType.TryGetValue(packetType, out var handler);
            return handler;
        }

        public PacketHeaderAttribute? GetPacketAttribute(Type packetType)
        {
            _attributeDic.TryGetValue(packetType, out var attr);
            return attr;
        }
    }
}
