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

        public override void Execute(SellPacket sellPacket, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrTrade)
            {
                //TODO log
                return;
            }

            if (sellPacket.Amount.HasValue && sellPacket.Slot.HasValue)
            {
                var type = (NoscorePocketType) sellPacket.Data;

                var inv = clientSession.Character.InventoryService.LoadBySlotAndType(sellPacket.Slot.Value, type);
                if ((inv == null) || (sellPacket.Amount.Value > inv.ItemInstance.Amount))
                {
                    //TODO log
                    return;
                }

                if (!inv.ItemInstance.Item.IsSoldable)
                {
                    clientSession.SendPacket(new SMemoPacket
                    {
                        Type = SMemoType.Error,
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_SOLDABLE,
                            clientSession.Account.Language)
                    });
                    return;
                }

                var price = inv.ItemInstance.Item.ItemType == ItemType.Sell ? inv.ItemInstance.Item.Price
                    : inv.ItemInstance.Item.Price / 20;

                if (clientSession.Character.Gold + price * sellPacket.Amount.Value > _worldConfiguration.MaxGoldAmount)
                {
                    clientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD,
                            clientSession.Account.Language),
                        Type = 0
                    });
                    return;
                }

                clientSession.Character.Gold += price * sellPacket.Amount.Value;
                clientSession.SendPacket(new SMemoPacket
                {
                    Type = SMemoType.Success,
                    Message = string.Format(
                        Language.Instance.GetMessageFromKey(LanguageKey.SELL_ITEM_VALIDE,
                            clientSession.Account.Language),
                        inv.ItemInstance.Item.Name[clientSession.Account.Language],
                        sellPacket.Amount.Value
                    )
                });

                clientSession.Character.InventoryService.RemoveItemAmountFromInventory(sellPacket.Amount.Value,
                    inv.ItemInstanceId);
                clientSession.SendPacket(clientSession.Character.GenerateGold());
            }
        }
    }
}