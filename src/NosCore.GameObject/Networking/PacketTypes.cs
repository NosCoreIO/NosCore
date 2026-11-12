//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using NosCore.Data.CommandPackets;
using NosCore.Packets;
using NosCore.Packets.Attributes;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Networking
{
    public static class PacketTypes
    {
        public static List<Type> LoginScreen() => ConcretePackets(typeof(IPacket).Assembly)
            .Where(p => p.GetCustomAttribute<PacketHeaderAttribute>() is not { } header || (header.Scopes & Scope.OnLoginScreen) != 0)
            .ToList();

        public static List<Type> World() => ConcretePackets(typeof(IPacket).Assembly)
            .Where(p => p.GetCustomAttribute<PacketHeaderAttribute>() is not { } header || (header.Scopes & Scope.OnLoginScreen) == 0)
            .Concat(ConcretePackets(typeof(HelpPacket).Assembly))
            .ToList();

        public static void RegisterPacketSerializers(this ContainerBuilder builder, List<Type> packetTypes)
        {
            builder.Register(_ => new Deserializer(packetTypes)).AsImplementedInterfaces().SingleInstance();
            builder.Register(_ => new Serializer(packetTypes)).AsImplementedInterfaces().SingleInstance();
        }

        private static IEnumerable<Type> ConcretePackets(Assembly assembly) =>
            assembly.GetTypes().Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract);
    }
}
