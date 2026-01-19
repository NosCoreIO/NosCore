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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.Tests.Shared.BDD.Steps
{
    public static class PacketSteps
    {
        public static async Task HandlePacketAsync<TPacket>(this ClientSession session, TPacket packet)
            where TPacket : class, IPacket
        {
            await session.HandlePacketsAsync(new IPacket[] { packet });
        }

        public static TPacket? GetLastPacket<TPacket>(this ClientSession session) where TPacket : class, IPacket
        {
            return (TPacket?)session.LastPackets.FirstOrDefault(s => s is TPacket);
        }

        public static void AssertNoPacketSent(this ClientSession session)
        {
            Assert.IsNull(session.LastPackets.FirstOrDefault());
        }

        public static void AssertReceivedMessage(this ClientSession session, Game18NConstString message)
        {
            var packet = session.GetLastPacket<MsgiPacket>();
            Assert.IsNotNull(packet, "Expected MsgiPacket but none was sent");
            Assert.AreEqual(message, packet.Message);
        }

        public static void AssertReceivedMessage(this ClientSession session, Game18NConstString message, MessageType type)
        {
            var packet = session.GetLastPacket<MsgiPacket>();
            Assert.IsNotNull(packet, "Expected MsgiPacket but none was sent");
            Assert.AreEqual(message, packet.Message);
            Assert.AreEqual(type, packet.Type);
        }

        public static void AssertReceivedModalMessage(this ClientSession session, Game18NConstString message)
        {
            var packet = session.GetLastPacket<ModaliPacket>();
            Assert.IsNotNull(packet, "Expected ModaliPacket but none was sent");
            Assert.AreEqual(message, packet.Message);
        }

        public static void AssertReceivedModalMessage(this ClientSession session, Game18NConstString message, int type, int argumentType)
        {
            var packet = session.GetLastPacket<ModaliPacket>();
            Assert.IsNotNull(packet, "Expected ModaliPacket but none was sent");
            Assert.AreEqual(message, packet.Message);
            Assert.AreEqual(type, packet.Type);
            Assert.AreEqual(argumentType, packet.ArgumentType);
        }

        public static void AssertReceivedPacket<TPacket>(this ClientSession session, Action<TPacket>? assertion = null)
            where TPacket : class, IPacket
        {
            var packet = session.GetLastPacket<TPacket>();
            Assert.IsNotNull(packet, $"Expected {typeof(TPacket).Name} but none was sent");
            assertion?.Invoke(packet);
        }
    }
}
