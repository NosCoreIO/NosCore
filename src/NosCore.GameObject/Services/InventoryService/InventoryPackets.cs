//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.InventoryService;

public static class InventoryPackets
{
    public static ExtsPacket GenerateExts(this PlayerComponentBundle player, IOptions<WorldConfiguration> conf)
    {
        return new ExtsPacket
        {
            EquipmentExtension = (byte)(player.InventoryService.Expensions[NoscorePocketType.Equipment] + conf.Value.BackpackSize),
            MainExtension = (byte)(player.InventoryService.Expensions[NoscorePocketType.Main] + conf.Value.BackpackSize),
            EtcExtension = (byte)(player.InventoryService.Expensions[NoscorePocketType.Etc] + conf.Value.BackpackSize)
        };
    }

    [Obsolete(
       "GenerateStartupInventory should be used only on startup, for refreshing an inventory slot please use GenerateInventoryAdd instead.")]
    public static IEnumerable<IPacket> GenerateInv(this PlayerComponentBundle characterEntity, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer)
    {
        var inv0 = new InvPacket { Type = PocketType.Equipment, IvnSubPackets = new List<IvnSubPacket>() };
        var inv1 = new InvPacket { Type = PocketType.Main, IvnSubPackets = new List<IvnSubPacket>() };
        var inv2 = new InvPacket { Type = PocketType.Etc, IvnSubPackets = new List<IvnSubPacket>() };
        var inv3 = new InvPacket { Type = PocketType.Miniland, IvnSubPackets = new List<IvnSubPacket>() };
        var inv6 = new InvPacket { Type = PocketType.Specialist, IvnSubPackets = new List<IvnSubPacket>() };
        var inv7 = new InvPacket { Type = PocketType.Costume, IvnSubPackets = new List<IvnSubPacket>() };
        var inv9 = new InvPacket { Type = PocketType.Mount, IvnSubPackets = new List<IvnSubPacket>() };
        var inv10 = new InvPacket { Type = PocketType.Raid, IvnSubPackets = new List<IvnSubPacket>() };

        if (characterEntity.InventoryService == null)
        {
            return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7, inv9, inv10 };
        }

        foreach (var inv in characterEntity.InventoryService.Select(s => s.Value))
        {
            switch (inv.Type)
            {
                case NoscorePocketType.Equipment:
                    if (inv.ItemInstance is SpecialistInstance specialistInstance)
                    {
                        inv0.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot,
                            VNum = inv.ItemInstance.ItemVNum,
                            RareAmount = specialistInstance.Rare,
                            UpgradeDesign = specialistInstance.Upgrade,
                            SecondUpgrade = specialistInstance.SpStoneUpgrade
                        });
                    }
                    else if (inv.ItemInstance is BoxInstance box)
                    {
                        inv0.IvnSubPackets.Add(box.GenerateIvnSubPacket(PocketType.Equipment, inv.Slot));
                    }
                    else if (inv.ItemInstance is WearableInstance wearableInstance)
                    {
                        inv0.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot,
                            VNum = inv.ItemInstance.ItemVNum,
                            RareAmount = wearableInstance.Rare,
                            UpgradeDesign = inv.ItemInstance.Item.IsColored ? wearableInstance.Design
                                : wearableInstance.Upgrade
                        });
                    }

                    break;

                case NoscorePocketType.Main:
                    inv1.IvnSubPackets.Add(new IvnSubPacket
                    {
                        Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                    });
                    break;

                case NoscorePocketType.Etc:
                    inv2.IvnSubPackets.Add(new IvnSubPacket
                    {
                        Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                    });
                    break;

                case NoscorePocketType.Miniland:
                    inv3.IvnSubPackets.Add(new IvnSubPacket
                    {
                        Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                    });
                    break;

                case NoscorePocketType.Specialist:
                    if (inv.ItemInstance is SpecialistInstance specialist)
                    {
                        inv6.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot,
                            VNum = inv.ItemInstance.ItemVNum,
                            RareAmount = specialist.Rare,
                            UpgradeDesign = specialist.Upgrade,
                            SecondUpgrade = specialist.SpStoneUpgrade
                        });
                    }

                    break;

                case NoscorePocketType.Costume:
                    if (inv.ItemInstance is WearableInstance costumeInstance)
                    {
                        inv7.IvnSubPackets.Add(new IvnSubPacket
                        {
                            Slot = inv.Slot,
                            VNum = inv.ItemInstance.ItemVNum,
                            RareAmount = costumeInstance.Rare,
                            UpgradeDesign = costumeInstance.Upgrade
                        });
                    }

                    break;

                case NoscorePocketType.Mount:
                    inv9.IvnSubPackets.Add(new IvnSubPacket
                    {
                        Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                    });
                    break;

                case NoscorePocketType.Raid:
                    inv10.IvnSubPackets.Add(new IvnSubPacket
                    {
                        Slot = inv.Slot, VNum = inv.ItemInstance.ItemVNum, RareAmount = inv.ItemInstance.Amount
                    });
                    break;

                case NoscorePocketType.Wear:
                case NoscorePocketType.FirstPartnerInventory:
                case NoscorePocketType.SecondPartnerInventory:
                case NoscorePocketType.ThirdPartnerInventory:
                    break;
                default:
                    logger.LogInformation(
                        logLanguageLocalizer[LogLanguageKey.POCKETTYPE_UNKNOWN]);
                    break;
            }
        }

        foreach (var pocket in new[] { inv0, inv1, inv2, inv3, inv6, inv7, inv9, inv10 })
        {
            pocket.IvnSubPackets?.ForEach(item => item.ShapedFor(pocket.Type));
        }

        return new List<IPacket> { inv0, inv1, inv2, inv3, inv6, inv7, inv9, inv10 };
    }
}
