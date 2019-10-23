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
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class VehicleEventHandlerTests
    {
        [TestInitialize]
        public void Setup()
        {
        }

        [TestMethod]
        public void Test_Can_Not_Vehicle_In_Shop()
        {
            //if (requestData.ClientSession.Character.InExchangeOrShop)
            //{
            //    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_USE_ITEM_IN_SHOP));
            //    return;
            //}
            Assert.Fail();
        }

        [TestMethod]
        public void Test_Vehicle_GetDelayed()
        {
            //if ((packet.Mode == 1) && !requestData.ClientSession.Character.IsVehicled)
            //{
            //    requestData.ClientSession.SendPacket(new DelayPacket
            //    {
            //        Type = 3,
            //        Delay = 3000,
            //        Packet = requestData.ClientSession.Character.GenerateUseItem((PocketType)itemInstance.Type,
            //            itemInstance.Slot,
            //            2, 0)
            //    });
            //    return;
            //}
            Assert.Fail();
        }

        [TestMethod]
        public void Test_Vehicle()
        {
            //if ((packet.Mode == 2) && !requestData.ClientSession.Character.IsVehicled)
            //{
            //    requestData.ClientSession.Character.IsVehicled = true;
            //    requestData.ClientSession.Character.VehicleSpeed = itemInstance.ItemInstance.Item.Speed;
            //    requestData.ClientSession.Character.MorphUpgrade = 0;
            //    requestData.ClientSession.Character.MorphDesign = 0;
            //    requestData.ClientSession.Character.Morph =
            //        itemInstance.ItemInstance.Item.SecondMorph == 0 ?
            //            (short)((short)requestData.ClientSession.Character.Gender +
            //                itemInstance.ItemInstance.Item.Morph) :
            //            requestData.ClientSession.Character.Gender == GenderType.Male
            //                ? itemInstance.ItemInstance.Item.Morph
            //                : itemInstance.ItemInstance.Item.SecondMorph;

            //    requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(
            //        requestData.ClientSession.Character.GenerateEff(196));
            //    requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character
            //        .GenerateCMode());
            //    requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateCond());
            //    return;
            //}

            Assert.Fail();
        }

        [TestMethod]
        public void Test_Vehicle_Remove()
        {
            //requestData.ClientSession.Character.RemoveVehicle();
            Assert.Fail();
        }
    }
}