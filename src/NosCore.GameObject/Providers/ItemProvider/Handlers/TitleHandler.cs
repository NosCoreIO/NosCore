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
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;

namespace NosCore.GameObject.Providers.ItemProvider.Handlers
{
    public class TitleHandler : IEventHandler<Item.Item, Tuple<InventoryItemInstance, UseItemPacket>>
    {
        public bool Condition(Item.Item item) => item.ItemType == ItemType.Title;

        public Task Execute(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            requestData.ClientSession.SendPacket(new QnaPacket
            {
                YesPacket = new GuriPacket
                {
                    Type = GuriPacketType.Title,
                    Unknown = (uint)requestData.Data.Item1.ItemInstance!.ItemVNum,
                    EntityId = requestData.Data.Item1.Slot
                },
                Question = Language.Instance.GetMessageFromKey(LanguageKey.WANT_ENABLE_TITLE,
                    requestData.ClientSession.Account.Language)
            });
            return Task.CompletedTask;
        }
    }
}