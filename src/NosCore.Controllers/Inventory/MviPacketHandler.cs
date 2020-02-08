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

using ChickenAPI.Packets.ClientPackets.Inventory;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.MapInstanceProvider;
using Serilog;
using System.Linq;

namespace NosCore.PacketHandlers.Inventory
{
    public class MviPacketHandler : PacketHandler<MviPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public MviPacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override void Execute(MviPacket mviPacket, ClientSession clientSession)
        {
            // check if the character is allowed to move the item
            if (clientSession.Character.InExchangeOrShop)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_MOVE_ITEM_IN_SHOP));
                return;
            }

            // check if the item is a palced MinilandObject
            if (mviPacket.InventoryType == ChickenAPI.Packets.Enumerations.PocketType.Miniland)
            {
                InventoryItemInstance sourceMinilandObject = clientSession.Character.InventoryService.LoadBySlotAndType(mviPacket.Slot, NoscorePocketType.Miniland);
                InventoryItemInstance destMinilandObject = clientSession.Character.InventoryService.LoadBySlotAndType(mviPacket.DestinationSlot, NoscorePocketType.Miniland);

                if (sourceMinilandObject != null || destMinilandObject != null)
                {
                    MapDesignObject designObject = 
                        clientSession.Character.MapInstance.MapDesignObjects.Values.FirstOrDefault(m => m.InventoryItemInstanceId == sourceMinilandObject?.Id);

                    MapDesignObject designObject2 =
                        clientSession.Character.MapInstance.MapDesignObjects.Values.FirstOrDefault(m => m.InventoryItemInstanceId == destMinilandObject?.Id);

                    if (designObject != null || designObject2 != null)
                    {
                        return;
                    }
                }
            }

            // actually move the item from source to destination
            clientSession.Character.InventoryService.TryMoveItem((NoscorePocketType) mviPacket.InventoryType, mviPacket.Slot,
                mviPacket.Amount,
                mviPacket.DestinationSlot, out var previousInventory, out var newInventory);
            clientSession.SendPacket(
                newInventory.GeneratePocketChange(mviPacket.InventoryType, mviPacket.DestinationSlot));
            clientSession.SendPacket(previousInventory.GeneratePocketChange(mviPacket.InventoryType, mviPacket.Slot));
        }
    }
}