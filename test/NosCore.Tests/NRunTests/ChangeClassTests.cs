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
using System.Linq;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.NRunProvider;
using NosCore.GameObject.Providers.NRunProvider.Handlers;
using System.Threading.Tasks;
using Serilog;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using ChickenAPI.Packets.ClientPackets.Npcs;
using NosCore.PacketHandlers.Shops;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.NRunTests
{
    [TestClass]
    public class ChangeClassTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private IItemProvider _item;
        private ClientSession _session;
        private NrunPacketHandler _nRunHandler;

        [TestInitialize]
        public void Setup()
        {
            _session = TestHelpers.Instance.GenerateSession();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _nRunHandler = new NrunPacketHandler(_logger, new NrunProvider(
                new List<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>
                    {new ChangeClassEventHandler()}));
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCantChangeClassLowLevel(CharacterClassType characterClass)
        {
            _session.Character.Level = 15;
            _nRunHandler.Execute(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            }, _session);

            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.TOO_LOW_LEVEL,
                _session.Account.Language) && packet.Type == MessageType.White);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCantChangeClassLowJobLevel(CharacterClassType characterClass)
        {
            _session.Character.JobLevel = 20;
            _nRunHandler.Execute(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            }, _session);

            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.TOO_LOW_LEVEL,
                _session.Account.Language) && packet.Type == MessageType.White);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCantChangeBadClass(CharacterClassType characterClass)
        {
            _session.Character.Class = characterClass;
            _nRunHandler.Execute(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)CharacterClassType.Swordman
            }, _session);

            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_ADVENTURER,
                _session.Account.Language) && packet.Type == MessageType.White);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.MartialArtist)]
        [DataRow(CharacterClassType.Adventurer)]
        public void UserCantChangeToBadClass(CharacterClassType characterClass)
        {
            _session.Character.Level = 15;
            _session.Character.JobLevel = 20;
            _nRunHandler.Execute(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            }, _session);

            Assert.IsTrue(_session.Character.Class == CharacterClassType.Adventurer && _session.Character.Level == 15 &&
                _session.Character.JobLevel == 20);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCanChangeClass(CharacterClassType characterClass)
        {
            _session.Character.Level = 15;
            _session.Character.JobLevel = 20;
            _nRunHandler.Execute(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            }, _session);

            Assert.IsTrue(_session.Character.Class == characterClass && _session.Character.Level == 15 &&
                _session.Character.JobLevel == 1);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCanNotChangeClassWhenEquipment(CharacterClassType characterClass)
        {
            _session.Character.Level = 15;
            _session.Character.JobLevel = 20;
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            var item = _session.Character.Inventory.First();
            item.Value.Type = PocketType.Wear;
            _nRunHandler.Execute(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            }, _session);

            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.EQ_NOT_EMPTY,
                _session.Account.Language) && packet.Type == MessageType.White);
        }
    }
}