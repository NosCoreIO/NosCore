//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.Packets.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

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
                var type = handler.PacketType;

                if (!_attributeDic.ContainsKey(type))
                {
                    var attr = type.GetCustomAttribute<PacketHeaderAttribute>(true);
                    if (attr != null)
                    {
                        _attributeDic.Add(type, attr);
                    }
                }

                // Two handlers claiming the same packet used to be swallowed here, with
                // whichever the container happened to yield first winning. NOSDI003 now
                // fails the build instead, so reaching this at runtime means a handler
                // was registered dynamically outside the generated lists.
                if (_handlersByPacketType.TryGetValue(type, out var existing))
                {
                    throw new InvalidOperationException(
                        $"Two handlers claim {type.Name}: {existing.GetType().Name} and {handler.GetType().Name}.");
                }

                _handlersByPacketType.Add(type, handler);
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
