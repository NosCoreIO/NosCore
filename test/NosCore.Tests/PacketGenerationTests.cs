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

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.CommandPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Tests
{
    [TestClass]
    public class PacketGenerationTests
    {
        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
        }

        [TestMethod]
        public void GenerateInPacketIsNotCorruptedForCharacter()
        {
            var characterTest = new Character {Name = "characterTest", Account = new AccountDto {Authority = AuthorityType.Administrator}, Level = 1};

            var packet = PacketFactory.Serialize(new[] {characterTest.GenerateIn("")});
            Assert.AreEqual(
                $"in 1 characterTest - 0 0 0 0 {(byte) characterTest.Authority} 0 0 0 0 -1.-1.-1.-1.-1.-1.-1.-1.-1 0 0 0 -1 0 0 0 0 0 0 0 0 -1 - 1 0 0 0 0 1 0 0 0 0 0",
                packet);
        }

        [TestMethod]
        public void GeneratePacketWithClientPacket()
        {
            var dlgTest = new DlgPacket
            {
                Question = "question", NoPacket = new FinsPacket {Type = FinsPacketType.Rejected, CharacterId = 1},
                YesPacket = new FinsPacket {Type = FinsPacketType.Accepted, CharacterId = 1}
            };

            var packet = PacketFactory.Serialize(new[] {dlgTest});
            Assert.AreEqual(
                "dlg #fins^1^1 #fins^2^1 question",
                packet);
        }

        [TestMethod]
        public void GeneratePacketWithSpecialSeparator()
        {
            var dlgTest = new BlinitPacket
            {
                SubPackets = new List<BlinitSubPacket>
                {
                    new BlinitSubPacket {RelatedCharacterId = 1, CharacterName = "test"},
                    new BlinitSubPacket {RelatedCharacterId = 2, CharacterName = "test2"}
                }
            };

            var packet = PacketFactory.Serialize(new[] {dlgTest});
            Assert.AreEqual(
                "blinit 1|test 2|test2",
                packet);
        }

        [TestMethod]
        public void GenerateInPacketIsNotCorruptedForMonster()
        {
            var mapMonsterTest = new MapMonster();

            var packet = PacketFactory.Serialize(new[] {mapMonsterTest.GenerateIn()});
            Assert.AreEqual("in 3 - 0 0 0 0 0 0 0 0 0 -1 0 0 -1 - 0 -1 0 0 0 0 0 0 0 0", packet);
        }

        [TestMethod]
        public void GeneratePacketWithDefaultSplitter()
        {
            var subpacket = new List<NsTeStSubPacket>
            {
                new NsTeStSubPacket
                {
                    Host = "-1",
                    Port = null,
                    Color = null,
                    WorldCount = 10000,
                    WorldId = 10000,
                    Name = "1"
                }
            };
            var nstestpacket = new NsTestPacket
            {
                AccountName = "test",
                SubPacket = subpacket,
                SessionId = 1
            };

            var packet = PacketFactory.Serialize(new[] {nstestpacket});
            Assert.AreEqual("NsTeST test 1 -1:-1:-1:10000.10000.1", packet);
        }

        [TestMethod]
        public void GenerateInPacketIsNotCorruptedForNpc()
        {
            var mapNpcTest = new MapNpc();

            var packet = PacketFactory.Serialize(new[] {mapNpcTest.GenerateIn()});
            Assert.AreEqual("in 2 - 0 0 0 0 0 0 0 0 0 -1 0 0 -1 - 0 -1 0 0 0 0 0 0 0 0", packet);
        }

        [TestMethod]
        public void GenerateInPacketIsNotCorruptedForItem()
        {
            var mapItemTest = new MapItem();

            var packet = PacketFactory.Serialize(new[] {mapItemTest.GenerateIn()});
            Assert.AreEqual($"in 9 - {mapItemTest.VisualId} 0 0 0 {mapItemTest.Amount} 0 0", packet);
        }

        [TestMethod]
        public void PacketEndingWithNullableMakeItOptional()
        {
            var packet = PacketFactory.Deserialize("$CreateItem 1012 1", typeof(CreateItemPacket));
            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void CheckWhisperIsNotCorrupted()
        {
            var packet = new WhisperPacket
            {
                Message = "test message !"
            };

            var serializedPacket = PacketFactory.Serialize(new[] {packet});
            Assert.AreEqual("/ test message !", serializedPacket);
        }

        [TestMethod]
        public void TestSerializeToEndCantBeNull()
        {
            var packet = new WhisperPacket
            {
                Message = null
            };

            var serializedPacket = PacketFactory.Serialize(new[] { packet });
            Assert.AreEqual(serializedPacket, "/ -");
        }
    }
}