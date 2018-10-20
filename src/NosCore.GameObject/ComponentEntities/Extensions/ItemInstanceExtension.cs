//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System.Collections.Generic;
using System.Linq;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class ItemInstanceExtension
    {
        public static IvnPacket GeneratePocketChange(this ItemInstance itemInstance, PocketType type, short slot)
        {
            if (itemInstance == null)
            {
                return new IvnPacket
                {
                    Type = type,
                    IvnSubPackets = new List<IvnSubPacket>
                    {
                        new IvnSubPacket()
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
                    new IvnSubPacket()
                    {
                        Slot = slot,
                        VNum = itemInstance.ItemVNum,
                        RareAmount =
                            itemInstance.Type != PocketType.Equipment ? itemInstance.Amount : itemInstance.Rare,
                        UpgradeDesign = itemInstance.Upgrade,
                        SecondUpgrade = 0
                    }
                }
            };
        }

        public static IvnPacket GeneratePocketChange(this List<ItemInstance> itemInstance)
        {
            if (itemInstance.Count > 0)
            {
                return new IvnPacket
                {
                    Type = itemInstance[0].Type,
                    IvnSubPackets = itemInstance.Select(item => new IvnSubPacket()
                    {
                        Slot = item.Slot,
                        VNum = item.ItemVNum,
                        RareAmount = item.Type != PocketType.Equipment ? item.Amount : item.Rare,
                        UpgradeDesign = item.Upgrade,
                        SecondUpgrade = 0
                    }).ToList()
                };
            }

            return null;
        }
    }
}