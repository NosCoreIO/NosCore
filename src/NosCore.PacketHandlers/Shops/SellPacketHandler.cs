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

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class SellPacketHandler(IOptions<WorldConfiguration> worldConfiguration) : PacketHandler<SellPacket>,
        IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SellPacket sellPacket, ClientSession clientSession)
        {
            var type = (NoscorePocketType)sellPacket.Data;

            var inv = clientSession.Character.InventoryService.LoadBySlotAndType(sellPacket.Slot, type);
            if ((inv == null) || (sellPacket.Amount > inv.ItemInstance!.Amount))
            {
                //TODO log
                return;
            }

            if (!inv.ItemInstance.Item!.IsSoldable)
            {
                await clientSession.SendPacketAsync(new SMemoiPacket
                {
                    Type = SMemoType.FailNpc,
                    Message = Game18NConstString.ItemCanNotBeSold
                }).ConfigureAwait(false);
                return;
            }

            var price = inv.ItemInstance.Item.ItemType == ItemType.Sell ? inv.ItemInstance.Item.Price
                : inv.ItemInstance.Item.Price / 20;

            if (clientSession.Character.Gold + price * sellPacket.Amount > worldConfiguration.Value.MaxGoldAmount)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.MaxGoldReached
                }).ConfigureAwait(false);
                return;
            }

            clientSession.Character.Gold += price * sellPacket.Amount;

            await clientSession.SendPacketAsync(new SMemoiPacket
            {
                Type = SMemoType.SuccessPlayer,
                Message = Game18NConstString.TradeSuccessfull
            }).ConfigureAwait(false);

            clientSession.Character.InventoryService.RemoveItemAmountFromInventory(sellPacket.Amount,
                inv.ItemInstanceId);
            await clientSession.SendPacketAsync(clientSession.Character.GenerateGold()).ConfigureAwait(false);
            await clientSession.SendPacketAsync(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot)).ConfigureAwait(false);
        }
    }
}