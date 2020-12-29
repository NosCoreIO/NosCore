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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;

//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class SpTransformPacketHandlerTests
    {
        private IItemGenerationService? _item;
        private ClientSession? _session;
        private SpTransformPacketHandler? _spTransformPacketHandler;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            SystemTime.Freeze();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _spTransformPacketHandler = new SpTransformPacketHandler();
        }


        [TestMethod]
        public async Task Test_Transform_NoSpAsync()
        {
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSp }, _session!).ConfigureAwait(false);
            var packet = (MsgPacket?)_session!.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.NO_SP, _session.Account.Language));
        }

        [TestMethod]
        public async Task Test_Transform_VehicleAsync()
        {
            _session!.Character.IsVehicled = true;
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1),
                _session.Character.CharacterId));
            var item = _session.Character.InventoryService.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSp }, _session).ConfigureAwait(false);
            var packet = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.CantUseInVehicle);
        }


        [TestMethod]
        public async Task Test_Transform_SittedAsync()
        {
            _session!.Character.IsSitting = true;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSp }, _session).ConfigureAwait(false);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task Test_RemoveSpAsync()
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
        public async Task Test_TransformAsync()
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
        public async Task Test_Transform_BadFairyAsync()
        {
            _session!.Character.SpPoint = 1;
            _session.Character.Reput = 5000000;
            var item = _session.Character.InventoryService!
                .AddItemToPocket(InventoryItemInstance.Create(_item!.Create(912, 1), _session.Character.CharacterId))!
                .First();
            var fairy = _session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(_item!.Create(2, 1), _session.Character.CharacterId))!
                .First();

            item.Type = NoscorePocketType.Wear;
            item.Slot = (byte)EquipmentType.Sp;
            fairy.Type = NoscorePocketType.Wear;
            fairy.Slot = (byte)EquipmentType.Fairy;
            await _spTransformPacketHandler!.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }, _session).ConfigureAwait(false);
            var packet = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.SpecialistAndFairyDifferentElement);
        }

        [TestMethod]
        public async Task Test_Transform_BadReputAsync()
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
        public async Task Test_TransformBefore_CooldownAsync()
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
        public async Task Test_Transform_OutOfSpPointAsync()
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
        public async Task Test_Transform_DelayAsync()
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