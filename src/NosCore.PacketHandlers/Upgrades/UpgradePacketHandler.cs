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
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Player;
using Serilog;
using System.Threading.Tasks;
using NosCore.Algorithm.SumService;
using System;
using NosCore.Shared.Helpers;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.GameObject.Networking.Group;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using System.Collections.Generic;

namespace NosCore.PacketHandlers.Upgrades
{
    public class UpgradePacketHandler : PacketHandler<UpgradePacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly ISumService _sumService;
        private const short SandVNum = 1027;

        public UpgradePacketHandler(ILogger logger, ISumService sumService)
        {
            _logger = logger;
            _sumService = sumService;
        }

        public override async Task ExecuteAsync(UpgradePacket upgradePacket, ClientSession session)
        {
            switch (upgradePacket.UpgradeType)
            {
                case UpgradePacketType.SumResistance:
                    if (upgradePacket.Slot2 == null)
                    {
                        var e = new InvalidOperationException("Upgrade packet received with Slot2 null!");
                        _logger.Error(e.Message, e);
                        return;
                    }

                    var sourceSlot = session.Character.InventoryService.LoadBySlotAndType(upgradePacket.Slot, (NoscorePocketType)upgradePacket.InventoryType);
                    var targetSlot = session.Character.InventoryService.LoadBySlotAndType((byte)upgradePacket.Slot2, (NoscorePocketType)upgradePacket.InventoryType);

                    if (sourceSlot == null || targetSlot == null || sourceSlot.ItemInstance is not WearableInstance sourceWearableInstance || targetSlot.ItemInstance is not WearableInstance targetWearableInstance)
                    {
                        var e = new InvalidOperationException("Upgrade packet received with null source, target, or not WearableInstance!");
                        _logger.Error(e.Message, e);
                        return;
                    }

                    if (sourceSlot.ItemInstance!.Upgrade + targetSlot.ItemInstance!.Upgrade > 5)
                    {
                        await session.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.CombinationNumExceeded
                        });
                        return;
                    }

                    var levelSum = (byte)(sourceSlot.ItemInstance.Upgrade + targetSlot.ItemInstance.Upgrade);
                    var sandCost = _sumService.GetSandCost(levelSum);
                    var price = _sumService.GetPrice(levelSum);
                    var successRate = _sumService.GetSuccessRate(levelSum);
                    var random = RandomHelper.Instance.RandomNumber();
                    if (random > successRate)
                    {
                        // Failed
                        session.Character.InventoryService.RemoveItemAmountFromInventory(1, sourceSlot.ItemInstanceId);
                        await session.SendPacketsAsync(new IPacket[] {
                            new GuriPacket
                            {
                                Type = GuriPacketType.AfterSumming,
                                Argument = 1,
                                SecondArgument = 0,
                                EntityId = session.Character.CharacterId,
                                Value = 1332
                            },
                            new MsgiPacket
                            {
                                Message = Game18NConstString.CombinationFailed
                            },
                            new Sayi2Packet
                            {
                                Message = Game18NConstString.CombinationItemsDisappeared,
                                Type = SayColorType.Purple
                            }
                        });
                    }
                    else
                    {
                        // Succed
                        sourceWearableInstance.DarkResistance = sourceWearableInstance.DarkResistance == null && targetWearableInstance.DarkResistance == null ? null :
                            (short?)((sourceWearableInstance.DarkResistance ?? 0) + targetWearableInstance.DarkResistance);
                        sourceWearableInstance.LightResistance = sourceWearableInstance.LightResistance == null && targetWearableInstance.LightResistance == null ? null :
                            (short?)((sourceWearableInstance.LightResistance ?? 0) + targetWearableInstance.LightResistance);
                        sourceWearableInstance.FireResistance = sourceWearableInstance.FireResistance == null && targetWearableInstance.FireResistance == null ? null :
                            (short?)((sourceWearableInstance.FireResistance ?? 0) + targetWearableInstance.FireResistance);
                        sourceWearableInstance.WaterResistance = sourceWearableInstance.WaterResistance == null && targetWearableInstance.WaterResistance == null ? null :
                            (short?)((sourceWearableInstance.WaterResistance ?? 0) + targetWearableInstance.WaterResistance);
                        sourceSlot.ItemInstance.Upgrade += (byte)(targetSlot.ItemInstance.Upgrade + 1);

                        await session.Character.SendPacketsAsync(new IPacket[] {
                            new GuriPacket
                            {
                                Type = GuriPacketType.AfterSumming,
                                Argument = 1,
                                SecondArgument = 0,
                                EntityId = session.Character.CharacterId,
                                Value = 1324
                            },
                            new PdtiPacket
                            {
                                Unknow = 10,
                                ItemVnum = sourceSlot.ItemInstance.ItemVNum,
                                RecipeAmount = 1,
                                Unknow3 = 27,
                                ItemUpgrade = sourceSlot.ItemInstance.Upgrade,
                                Unknow4 = 0
                            },
                            new MsgiPacket
                            {
                                Message = Game18NConstString.CombinationSuccessful
                            },
                            new Sayi2Packet
                            {
                                Message = Game18NConstString.CombinationSuccessful,
                                Type = SayColorType.Green
                            }
                        });
                    }

                    var removeSandInstances = session.Character.InventoryService.RemoveItemAmountFromInventory((short)sandCost, SandVNum);
                    if (removeSandInstances == null)
                    {
                        var e = new InvalidOperationException("Remove sand failed!");
                        _logger.Error(e.Message, e);
                        return;
                    }

                    session.Character.InventoryService.DeleteById(targetSlot.ItemInstanceId);
                    await session.Character.RemoveGoldAsync(price);

                    var targetSlotValue = targetSlot.Slot;
                    targetSlot = null;
                    var removePackets = new List<IPacket>
                    {
                        sourceSlot.GeneratePocketChange(PocketType.Equipment, sourceSlot.Slot),
                        targetSlot.GeneratePocketChange(PocketType.Equipment, targetSlotValue),
                        targetSlot.GeneratePocketChange(PocketType.Equipment, targetSlotValue),
                        new ShopEndPacket
                        {
                            Type = ShopEndPacketType.CloseSubWindow
                        }
                    };
                    removeSandInstances!.ForEach((s) =>
                    {
                        targetSlotValue = s.Slot;
                        if (s!.ItemInstance!.Amount == 0)
                        {
                            targetSlotValue = s.Slot;
                        }
                        removePackets.Add(s.GeneratePocketChange(PocketType.Main, targetSlotValue));
                    });
                    await session.Character.SendPacketsAsync(removePackets);
                    await session.Character.MapInstance.SendPacketAsync(new GuriPacket
                    {
                        Type = GuriPacketType.Unknow,
                        Argument = 1,
                        SecondArgument = 0,
                        EntityId = session.Character.CharacterId
                    });
                    break;
            }
        }
    }
}