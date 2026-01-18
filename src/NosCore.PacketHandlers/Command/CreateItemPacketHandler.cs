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
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class CreateItemPackettHandler(ILogger logger, List<ItemDto> items,
            IOptions<WorldConfiguration> worldConfiguration,
            IItemGenerationService itemProvider, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IInventoryPacketSystem inventoryPacketSystem)
        : PacketHandler<CreateItemPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CreateItemPacket createItemPacket, ClientSession session)
        {
            var vnum = createItemPacket.VNum;
            sbyte rare = 0;
            byte upgrade = 0;
            byte design = 0;
            short amount = 1;
            if (vnum == 1046)
            {
                return; // cannot Create gold as item, use $Gold instead
            }

            var iteminfo = items.Find(item => item.VNum == vnum);
            if (iteminfo == null)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.ItemDoesNotExist
                }).ConfigureAwait(false);
                return;
            }

            if (iteminfo.IsColored || (iteminfo.Effect == ItemEffectType.BoxEffect))
            {
                if (createItemPacket.DesignOrAmount.HasValue)
                {
                    design = (byte)createItemPacket.DesignOrAmount.Value;
                }

                rare = createItemPacket.Upgrade.HasValue && (iteminfo.Effect == ItemEffectType.BoxEffect)
                    ? (sbyte)createItemPacket.Upgrade.Value : rare;
            }
            else if (iteminfo.Type == NoscorePocketType.Equipment)
            {
                if (createItemPacket.Upgrade.HasValue)
                {
                    if (iteminfo.EquipmentSlot != EquipmentType.Sp)
                    {
                        upgrade = createItemPacket.Upgrade.Value;
                    }
                    else
                    {
                        design = createItemPacket.Upgrade.Value;
                    }

                    if ((iteminfo.EquipmentSlot != EquipmentType.Sp) && (upgrade == 0)
                        && (iteminfo.BasicUpgrade != 0))
                    {
                        upgrade = iteminfo.BasicUpgrade;
                    }
                }

                if (createItemPacket.DesignOrAmount.HasValue)
                {
                    if (iteminfo.EquipmentSlot == EquipmentType.Sp)
                    {
                        upgrade = (byte)createItemPacket.DesignOrAmount.Value;
                    }
                    else if (iteminfo.EquipmentSlot == EquipmentType.Armor || iteminfo.EquipmentSlot == EquipmentType.MainWeapon || iteminfo.EquipmentSlot == EquipmentType.SecondaryWeapon)
                    {
                        rare = (sbyte)createItemPacket.DesignOrAmount.Value;
                    }
                }
            }
            else if (createItemPacket.DesignOrAmount.HasValue && !createItemPacket.Upgrade.HasValue)
            {
                amount = createItemPacket.DesignOrAmount.Value > worldConfiguration.Value.MaxItemAmount
                    ? worldConfiguration.Value.MaxItemAmount : createItemPacket.DesignOrAmount.Value;
            }

            var inv = session.Player.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemProvider.Create(
                vnum,
                amount, rare, upgrade, design), session.Player.CharacterId));

            if (inv == null || inv.Count <= 0)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                }).ConfigureAwait(false);
                return;
            }

            await session.SendPacketAsync(inventoryPacketSystem.GeneratePocketChange(inv)).ConfigureAwait(false);
            var firstItem = inv[0];

            if (session.Player.InventoryService.LoadBySlotAndType(firstItem.Slot,
                    firstItem.Type)!.ItemInstance is WearableInstance wearable)
            {
                switch (wearable.Item.EquipmentSlot)
                {
                    case EquipmentType.Armor:
                    case EquipmentType.MainWeapon:
                    case EquipmentType.SecondaryWeapon:
                        wearable.SetRarityPoint();
                        break;
                    case EquipmentType.Boots:
                    case EquipmentType.Gloves:
                        wearable.FireResistance = (short)(wearable.Item.FireResistance * upgrade);
                        wearable.DarkResistance = (short)(wearable.Item.DarkResistance * upgrade);
                        wearable.LightResistance = (short)(wearable.Item.LightResistance * upgrade);
                        wearable.WaterResistance = (short)(wearable.Item.WaterResistance * upgrade);
                        break;
                    default:
                        logger.Debug(
                            logLanguage[LogLanguageKey.NO_SPECIAL_PROPERTIES_WEARABLE]);
                        break;
                }
            }

            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = session.Player.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.ReceivedThisItem,
                ArgumentType = 2,
                Game18NArguments = { iteminfo.VNum.ToString(), amount }
            }).ConfigureAwait(false);
        }
    }
}