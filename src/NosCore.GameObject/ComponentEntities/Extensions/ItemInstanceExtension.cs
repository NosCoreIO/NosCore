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

using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Inventory;
using NosCore.Data;
using NosCore.GameObject.Providers.InventoryService;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class IItemInstanceExtension
    {
        public static IvnPacket GeneratePocketChange(this InventoryItemInstance itemInstance, PocketType type, short slot)
        {
            if (itemInstance == null)
            {
                return new IvnPacket
                {
                    Type = type,
                    IvnSubPackets = new List<IvnSubPacket>
                    {
                        new IvnSubPacket
                        {
                            Slot = slot,
                            VNum = -1,
                            RareAmount = 0,
                            UpgradeDesign = 0,
                            SecondUpgrade = 0
                        }
                    }
                };
            }

            return new IvnPacket
            {
                Type = type,
                IvnSubPackets = new List<IvnSubPacket>
                {
                    new IvnSubPacket
                    {
                        Slot = slot,
                        VNum = itemInstance.ItemInstance.ItemVNum,
                        RareAmount =
                            itemInstance.Type != NoscorePocketType.Equipment ? itemInstance.ItemInstance.Amount : itemInstance.ItemInstance.Rare,
                        UpgradeDesign = itemInstance.ItemInstance.Upgrade,
                        SecondUpgrade = 0
                    }
                }
            };
        }

        public static IvnPacket GeneratePocketChange(this List<InventoryItemInstance> itemInstance)
        {
            if (itemInstance.Count > 0)
            {
                return new IvnPacket
                {
                    Type = (PocketType)itemInstance[0].Type,
                    IvnSubPackets = itemInstance.Select(item => new IvnSubPacket
                    {
                        Slot = item.Slot,
                        VNum = item.ItemInstance.ItemVNum,
                        RareAmount = item.Type != NoscorePocketType.Equipment ? item.ItemInstance.Amount : item.ItemInstance.Rare,
                        UpgradeDesign = item.ItemInstance.Upgrade,
                        SecondUpgrade = 0
                    }).ToList()
                };
            }

            return null;
        }
    }
}