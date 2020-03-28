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
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.NRunProvider;
using NosCore.GameObject.Providers.NRunProvider.Handlers;
using NosCore.PacketHandlers.Shops;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.NRunTests
{
    [TestClass]
    public class ChangeClassTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private IItemProvider _item;
        private NrunPacketHandler _nRunHandler;
        private ClientSession _session;

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
                Type = (byte) characterClass
            }, _session);

            var packet = (MsgPacket?) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue((packet?.Message == Language.Instance.GetMessageFromKey(LanguageKey.TOO_LOW_LEVEL,
                _session.Account.Language)) && (packet.Type == MessageType.White));
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
                Type = (byte) characterClass
            }, _session);

            var packet = (MsgPacket?) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue((packet?.Message == Language.Instance.GetMessageFromKey(LanguageKey.TOO_LOW_LEVEL,
                _session.Account.Language)) && (packet.Type == MessageType.White));
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
                Type = (byte) CharacterClassType.Swordman
            }, _session);

            var packet = (MsgPacket?) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue((packet?.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_ADVENTURER,
                _session.Account.Language)) && (packet.Type == MessageType.White));
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
                Type = (byte) characterClass
            }, _session);

            Assert.IsTrue((_session.Character.Class == CharacterClassType.Adventurer) &&
                (_session.Character.Level == 15) &&
                (_session.Character.JobLevel == 20));
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
                Type = (byte) characterClass
            }, _session);

            Assert.IsTrue((_session.Character.Class == characterClass) && (_session.Character.Level == 15) &&
                (_session.Character.JobLevel == 1));
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCanNotChangeClassWhenEquipment(CharacterClassType characterClass)
        {
            _session.Character.Level = 15;
            _session.Character.JobLevel = 20;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1), 0));
            var item = _session.Character.InventoryService.First();
            item.Value.Type = NoscorePocketType.Wear;
            _nRunHandler.Execute(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte) characterClass
            }, _session);

            var packet = (MsgPacket?) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue((packet?.Message == Language.Instance.GetMessageFromKey(LanguageKey.EQ_NOT_EMPTY,
                _session.Account.Language)) && (packet.Type == MessageType.White));
        }
    }
}