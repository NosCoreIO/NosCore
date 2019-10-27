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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NosCore.Tests.ParcelTests
{
    [TestClass]
    public class PclPacketHandlerTests
    {
        [TestInitialize]
        public void Setup()
        {
        }

        [TestMethod]
        public void Test_GiftNotFound()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void Test_DeleteGift()
        {
            //else if (getGiftPacket.Type == 5)
            //{
            //    clientSession.SendPacket(new ParcelPacket { Type = 7, Unknown = 1, Id = (short)getGiftPacket.GiftId });
            //    _mailHttpClient.DeleteGift(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy);
            //}
            Assert.Fail();
        }

        [TestMethod]
        public void Test_ReceiveGift()
        {
            //if ((getGiftPacket.Type == 4) && (mail.ItemInstance != null))
            //{
            //    var itemInstance = _itemInstanceDao.FirstOrDefault(s => s.Id == mail.ItemInstance.Id);
            //    var item = _itemProvider.Convert(itemInstance);
            //    item.Id = Guid.NewGuid();
            //    var newInv = clientSession.Character.Inventory
            //        .AddItemToPocket(InventoryItemInstance.Create(item, clientSession.Character.CharacterId))
            //        .FirstOrDefault();
            //    if (newInv != null)
            //    {
            //        clientSession.SendPacket(clientSession.Character.GenerateSay(
            //            string.Format(
            //                Language.Instance.GetMessageFromKey(LanguageKey.ITEM_RECEIVED,
            //                    clientSession.Account.Language),
            //                newInv.ItemInstance.Item.Name, newInv.ItemInstance.Amount), SayColorType.Green));
            //        clientSession.SendPacket(
            //            new ParcelPacket { Type = 2, Unknown = 1, Id = (short)getGiftPacket.GiftId });
            //        _mailHttpClient.DeleteGift(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy);
            //    }
            //    else
            //    {
            //        clientSession.SendPacket(new ParcelPacket { Type = 5, Unknown = 1, Id = 0 });
            //        clientSession.SendPacket(new MsgPacket
            //        {
            //            Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
            //                clientSession.Account.Language),
            //            Type = 0
            //        });
            //    }
            //}
            Assert.Fail();
        }

        [TestMethod]
        public void Test_ReceiveGiftNoPlace()
        {
            //if ((getGiftPacket.Type == 4) && (mail.ItemInstance != null))
            //{
            //    var itemInstance = _itemInstanceDao.FirstOrDefault(s => s.Id == mail.ItemInstance.Id);
            //    var item = _itemProvider.Convert(itemInstance);
            //    item.Id = Guid.NewGuid();
            //    var newInv = clientSession.Character.Inventory
            //        .AddItemToPocket(InventoryItemInstance.Create(item, clientSession.Character.CharacterId))
            //        .FirstOrDefault();
            //    if (newInv != null)
            //    {
            //        clientSession.SendPacket(clientSession.Character.GenerateSay(
            //            string.Format(
            //                Language.Instance.GetMessageFromKey(LanguageKey.ITEM_RECEIVED,
            //                    clientSession.Account.Language),
            //                newInv.ItemInstance.Item.Name, newInv.ItemInstance.Amount), SayColorType.Green));
            //        clientSession.SendPacket(
            //            new ParcelPacket { Type = 2, Unknown = 1, Id = (short)getGiftPacket.GiftId });
            //        _mailHttpClient.DeleteGift(getGiftPacket.GiftId, clientSession.Character.VisualId, isCopy);
            //    }
            //    else
            //    {
            //        clientSession.SendPacket(new ParcelPacket { Type = 5, Unknown = 1, Id = 0 });
            //        clientSession.SendPacket(new MsgPacket
            //        {
            //            Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
            //                clientSession.Account.Language),
            //            Type = 0
            //        });
            //    }
            //}
            Assert.Fail();
        }
    }
}