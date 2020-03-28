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
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using Serilog;

namespace NosCore.PacketHandlers.Inventory
{
    public class MvePacketHandler : PacketHandler<MvePacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public MvePacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override Task Execute(MvePacket mvePacket, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrShop)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_MOVE_ITEM_IN_SHOP));
                return Task.CompletedTask;
            }

            var inv = clientSession.Character.InventoryService.MoveInPocket(mvePacket.Slot,
                (NoscorePocketType) mvePacket.InventoryType,
                (NoscorePocketType) mvePacket.DestinationInventoryType, mvePacket.DestinationSlot, false);
            clientSession.SendPacket(inv.GeneratePocketChange(mvePacket.DestinationInventoryType,
                mvePacket.DestinationSlot));
            clientSession.SendPacket(
                ((InventoryItemInstance) null).GeneratePocketChange(mvePacket.InventoryType, mvePacket.Slot));
            return Task.CompletedTask;
        }
    }
}