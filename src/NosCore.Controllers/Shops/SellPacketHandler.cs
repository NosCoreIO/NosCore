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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Shops
{
    public class SellPacketHandler : PacketHandler<SellPacket>, IWorldPacketHandler
    {
        private readonly WorldConfiguration _worldConfiguration;

        public SellPacketHandler(WorldConfiguration worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
        }

        public override Task ExecuteAsync(SellPacket sellPacket, ClientSession clientSession)
        {
            var type = (NoscorePocketType) sellPacket.Data;

            var inv = clientSession.Character.InventoryService.LoadBySlotAndType(sellPacket.Slot, type);
            if ((inv == null) || (sellPacket.Amount > inv.ItemInstance!.Amount))
            {
                //TODO log
                return Task.CompletedTask;
            }

            if (!inv.ItemInstance.Item!.IsSoldable)
            {
                clientSession.SendPacketAsync(new SMemoPacket
                {
                    Type = SMemoType.Error,
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_SOLDABLE,
                        clientSession.Account.Language)
                });
                return Task.CompletedTask;
            }

            var price = inv.ItemInstance.Item.ItemType == ItemType.Sell ? inv.ItemInstance.Item.Price
                : inv.ItemInstance.Item.Price / 20;

            if (clientSession.Character.Gold + price * sellPacket.Amount > _worldConfiguration.MaxGoldAmount)
            {
                clientSession.SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD,
                        clientSession.Account.Language),
                    Type = 0
                });
                return Task.CompletedTask;
            }

            clientSession.Character.Gold += price * sellPacket.Amount;
            clientSession.SendPacketAsync(new SMemoPacket
            {
                Type = SMemoType.Success,
                Message = string.Format(
                    GameLanguage.Instance.GetMessageFromKey(LanguageKey.SELL_ITEM_VALIDE,
                        clientSession.Account.Language),
                    inv.ItemInstance.Item.Name[clientSession.Account.Language],
                    sellPacket.Amount
                )
            });

            clientSession.Character.InventoryService.RemoveItemAmountFromInventory(sellPacket.Amount,
                inv.ItemInstanceId);
            clientSession.SendPacketAsync(clientSession.Character.GenerateGold());
            return Task.CompletedTask;
        }
    }
}