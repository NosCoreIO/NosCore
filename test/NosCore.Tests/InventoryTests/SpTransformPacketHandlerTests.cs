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

using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.Inventory;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.InventoryTests
{
    [TestClass]
    public class SpTransformPacketHandlerTests
    {
        private IItemProvider? _item;
        private ClientSession? _session;
        private SpTransformPacketHandler? _spTransformPacketHandler;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            TestHelpers.Reset();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = TestHelpers.Instance.GenerateSession();
            _spTransformPacketHandler = new SpTransformPacketHandler();
        }


        [TestMethod]
        public async Task Test_Transform_NoSp()
        {
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSp }, _session!).ConfigureAwait(false);
            var packet = (MsgPacket?)_session!.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.NO_SP, _session.Account.Language));
        }

        [TestMethod]
        public async Task Test_Transform_Vehicle()
        {
            _session!.Character.IsVehicled = true;
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1),
                _session.Character.CharacterId));
            var item = _session.Character.InventoryService.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSp }, _session).ConfigureAwait(false);
            var packet = (MsgPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.REMOVE_VEHICLE, _session.Account.Language));
        }


        [TestMethod]
        public async Task Test_Transform_Sitted()
        {
            _session!.Character.IsSitting = true;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSp }, _session).ConfigureAwait(false);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task Test_RemoveSp()
        {
            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1),
                _session.Character.CharacterId));
            var item = _session.Character.InventoryService.First();
            _session.Character.UseSp = true;
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }, _session).ConfigureAwait(false);
            Assert.IsFalse(_session.Character.UseSp);
        }

        [TestMethod]
        public async Task Test_Transform()
        {
            _session!.Character.SpPoint = 1;
            _session.Character.Reput = 5000000;
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1),
                _session.Character.CharacterId));
            var item = _session.Character.InventoryService.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.UseSp);
        }

        [TestMethod]
        public async Task Test_Transform_BadFairy()
        {
            _session!.Character.SpPoint = 1;
            _session.Character.Reput = 5000000;
            var item = _session.Character.InventoryService!
                .AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1), _session.Character.CharacterId))
                .First();
            var fairy = _session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(_item!.Create(2, 1), _session.Character.CharacterId))
                .First();

            item.Type = NoscorePocketType.Wear;
            item.Slot = (byte)EquipmentType.Sp;
            fairy.Type = NoscorePocketType.Wear;
            fairy.Slot = (byte)EquipmentType.Fairy;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }, _session).ConfigureAwait(false);
            var packet = (MsgPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY, _session.Account.Language));
        }

        [TestMethod]
        public async Task Test_Transform_BadReput()
        {
            _session!.Character.SpPoint = 1;
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1),
                _session.Character.CharacterId));
            var item = _session.Character.InventoryService.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }, _session).ConfigureAwait(false);
            var packet = (MsgPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.LOW_REP, _session.Account.Language));
        }


        [TestMethod]
        public async Task Test_TransformBefore_Cooldown()
        {
            _session!.Character.SpPoint = 1;
            _session.Character.LastSp = SystemTime.Now();
            _session.Character.SpCooldown = 30;
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1),
                _session.Character.CharacterId));
            var item = _session.Character.InventoryService.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }, _session).ConfigureAwait(false);
            var packet = (MsgPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message ==
                string.Format(GameLanguage.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING, _session.Account.Language),
                    30));
        }

        [TestMethod]
        public async Task Test_Transform_OutOfSpPoint()
        {
            _session!.Character.LastSp = SystemTime.Now();
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1),
                _session.Character.CharacterId));
            var item = _session.Character.InventoryService.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }, _session).ConfigureAwait(false);
            var packet = (MsgPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.SP_NOPOINTS, _session.Account.Language));
        }

        [TestMethod]
        public async Task Test_Transform_Delay()
        {
            _session!.Character.SpPoint = 1;
            _session.Character.LastSp = SystemTime.Now();
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1),
                _session.Character.CharacterId));
            var item = _session.Character.InventoryService.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSp }, _session).ConfigureAwait(false);
            var packet = (DelayPacket?)_session.LastPackets.FirstOrDefault(s => s is DelayPacket);
            Assert.IsTrue(packet?.Delay == 5000);
        }
    }
}