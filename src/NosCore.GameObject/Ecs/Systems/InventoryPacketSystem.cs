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

using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Ecs.Systems;

public interface IInventoryPacketSystem
{
    IvnSubPacket GenerateIvnSubPacket(IItemInstance? itemInstance, PocketType type, short slot);
    IvnPacket GeneratePocketChange(InventoryItemInstance? itemInstance, PocketType type, short slot);
    IvnPacket? GeneratePocketChange(List<InventoryItemInstance> itemInstance);
}

public class InventoryPacketSystem : IInventoryPacketSystem
{
    public IvnSubPacket GenerateIvnSubPacket(IItemInstance? itemInstance, PocketType type, short slot)
    {
        if (itemInstance == null)
        {
            return new IvnSubPacket
            {
                Slot = slot,
                VNum = -1,
                RareAmount = 0,
                UpgradeDesign = 0,
                SecondUpgrade = 0
            };
        }

        return new IvnSubPacket
        {
            Slot = slot,
            VNum = itemInstance.ItemVNum,
            RareAmount = type != PocketType.Equipment ? itemInstance.Amount : itemInstance.Rare,
            UpgradeDesign = itemInstance.Upgrade,
            SecondUpgrade = 0
        };
    }

    public IvnPacket GeneratePocketChange(InventoryItemInstance? itemInstance, PocketType type, short slot)
    {
        if (itemInstance == null)
        {
            return new IvnPacket
            {
                Type = type,
                IvnSubPackets = new List<IvnSubPacket?> { GenerateIvnSubPacket(null, type, slot) }
            };
        }

        return new IvnPacket
        {
            Type = type,
            IvnSubPackets = new List<IvnSubPacket?>
            {
                new()
                {
                    Slot = slot,
                    VNum = itemInstance.ItemInstance.ItemVNum,
                    RareAmount = itemInstance.Type != NoscorePocketType.Equipment
                        ? itemInstance.ItemInstance.Amount
                        : itemInstance.ItemInstance.Rare,
                    UpgradeDesign = itemInstance.ItemInstance.Upgrade,
                    SecondUpgrade = 0
                }
            }
        };
    }

    public IvnPacket? GeneratePocketChange(List<InventoryItemInstance> itemInstance)
    {
        if (itemInstance.Count <= 0)
        {
            return null;
        }

        var type = (PocketType)itemInstance[0].Type;
        return new IvnPacket
        {
            Type = type,
            IvnSubPackets = itemInstance.Select(item =>
                GenerateIvnSubPacket(item.ItemInstance, type, item.Slot)).ToList() as List<IvnSubPacket?>
        };
    }
}
