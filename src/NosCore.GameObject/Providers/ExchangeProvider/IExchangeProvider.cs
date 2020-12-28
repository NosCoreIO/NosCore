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
using System.Collections.Generic;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;

namespace NosCore.GameObject.Providers.ExchangeProvider
{
    public interface IExchangeProvider
    {
        void SetGold(long visualId, long gold, long bankGold);

        Tuple<ExchangeResultType, Dictionary<long, IPacket>?> ValidateExchange(ClientSession session,
            ICharacterEntity targetSession);

        void ConfirmExchange(long visualId);
        bool IsExchangeConfirmed(long visualId);
        ExchangeData GetData(long visualId);
        void AddItems(long visualId, InventoryItemInstance item, short amount);
        bool CheckExchange(long visualId);
        long? GetTargetId(long visualId);
        bool CheckExchange(long visualId, long targetId);
        ExcClosePacket? CloseExchange(long visualId, ExchangeResultType resultType);
        bool OpenExchange(long visualId, long targetVisualId);

        List<KeyValuePair<long, IvnPacket>> ProcessExchange(long firstUser, long secondUser,
            IInventoryService sessionInventory, IInventoryService targetInventory);
    }
}