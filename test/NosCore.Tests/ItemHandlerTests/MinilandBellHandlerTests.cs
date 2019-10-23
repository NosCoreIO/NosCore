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
using ChickenAPI.Packets.ServerPackets.Chats;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.MinilandProvider;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class MinilandBellHandlerTests
    {
        [TestInitialize]
        public void Setup()
        {
        }

        [TestMethod]
        public void Test_Miniland_On_Instance()
        {
            //if (requestData.ClientSession.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
            //{
            //    requestData.ClientSession.Character.SendPacket(new SayPacket
            //    {
            //        Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_USE,
            //            requestData.ClientSession.Character.Account.Language),
            //        Type = SayColorType.Yellow
            //    });
            //    return;
            //}

            Assert.Fail();
        }

        [TestMethod]
        public void Test_Miniland_On_Vehicle()
        {
            //if (requestData.ClientSession.Character.IsVehicled)
            //{
            //    requestData.ClientSession.Character.SendPacket(new SayPacket
            //    {
            //        Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_USE_IN_VEHICLE,
            //            requestData.ClientSession.Character.Account.Language),
            //        Type = SayColorType.Yellow
            //    });
            //    return;
            //}

            Assert.Fail();
        }

        [TestMethod]
        public void Test_Miniland_Delay()
        {
            //if (packet.Mode == 0)
            //{
            //    requestData.ClientSession.SendPacket(new DelayPacket
            //    {
            //        Type = 3,
            //        Delay = 5000,
            //        Packet = requestData.ClientSession.Character.GenerateUseItem((PocketType)itemInstance.Type,
            //            itemInstance.Slot,
            //            2, 0)
            //    });
            //    return;
            //}

            Assert.Fail();
        }

        [TestMethod]
        public void Test_Miniland()
        {
        //    requestData.ClientSession.Character.Inventory.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
        //    requestData.ClientSession.SendPacket(
        //        itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
        //    var miniland = _minilandProvider.GetMiniland(requestData.ClientSession.Character.CharacterId);
        //    requestData.ClientSession.ChangeMapInstance(miniland.MapInstanceId, 5, 8);

            Assert.Fail();
        }
    }
}