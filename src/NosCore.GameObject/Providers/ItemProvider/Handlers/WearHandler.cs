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
using NosCore.Core;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Providers.ItemProvider.Handlers
{
    public class WearHandler : IHandler<Item.Item, Tuple<IItemInstance, UseItemPacket>>
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public bool Condition(Item.Item item) => item.ItemType == ItemType.Weapon
            || item.ItemType == ItemType.Jewelery
            || item.ItemType == ItemType.Armor
            || item.ItemType == ItemType.Fashion
            || item.ItemType == ItemType.Specialist;

        public void Execute(RequestData<Tuple<IItemInstance, UseItemPacket>> requestData)
        {
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateEff(123));

            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;
            if (requestData.ClientSession.Character.InExchangeOrShop)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_USE_ITEM_IN_SHOP));
                return;
            }

            if (itemInstance.BoundCharacterId == null)
            {
                if (packet.Mode == 0 && itemInstance.Item.RequireBinding)
                {
                    requestData.ClientSession.SendPacket(
                        new QnaPacket
                        {
                            YesPacket = requestData.ClientSession.Character.GenerateUseItem(itemInstance.Type,
                                itemInstance.Slot, (byte) packet.Mode, (byte) packet.Parameter),
                            Question = requestData.ClientSession.GetMessageFromKey(LanguageKey.ASK_BIND)
                        });
                    return;
                }

                if (packet.Mode != 0)
                {
                    itemInstance.BoundCharacterId = requestData.ClientSession.Character.CharacterId;
                }
            }

            if (itemInstance.Item.LevelMinimum > (itemInstance.Item.IsHeroic
                    ? requestData.ClientSession.Character.HeroLevel : requestData.ClientSession.Character.Level)
                || itemInstance.Item.Sex != 0 &&
                ((itemInstance.Item.Sex >> (byte) requestData.ClientSession.Character.Gender) & 1) != 1
                || itemInstance.Item.Class != 0 &&
                ((itemInstance.Item.Class >> (byte) requestData.ClientSession.Character.Class) & 1) != 1)
            {
                requestData.ClientSession.SendPacket(
                    requestData.ClientSession.Character.GenerateSay(
                        requestData.ClientSession.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT),
                        Shared.Enumerations.SayColorType.Yellow));
                return;
            }

            if (requestData.ClientSession.Character.UseSp && itemInstance.Item.EquipmentSlot == EquipmentType.Fairy)
            {
                var sp = requestData.ClientSession.Character.Inventory.LoadBySlotAndType<IItemInstance>(
                    (byte) EquipmentType.Sp, PocketType.Wear);

                if (sp != null && sp.Item.Element != 0 && itemInstance.Item.Element != sp.Item.Element &&
                    itemInstance.Item.Element != sp.Item.SecondaryElement)
                {
                    requestData.ClientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY,
                            requestData.ClientSession.Account.Language)
                    });
                    return;
                }
            }

            if (itemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                double timeSpanSinceLastSpUsage =
                    (SystemTime.Now() - requestData.ClientSession.Character.LastSp).TotalSeconds;
                var sp = requestData.ClientSession.Character.Inventory.LoadBySlotAndType<IItemInstance>(
                    (byte) EquipmentType.Sp, PocketType.Wear);
                if (timeSpanSinceLastSpUsage < requestData.ClientSession.Character.SpCooldown && sp != null)
                {
                    requestData.ClientSession.SendPacket(new MsgPacket
                    {
                        Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING,
                                requestData.ClientSession.Account.Language), requestData.ClientSession.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage))
                    });
                    return;
                }

                if (requestData.ClientSession.Character.UseSp)
                {
                    requestData.ClientSession.SendPacket(
                        requestData.ClientSession.Character.GenerateSay(
                            requestData.ClientSession.GetMessageFromKey(LanguageKey.SP_BLOCKED),
                            Shared.Enumerations.SayColorType.Yellow));
                    return;
                }

                if (itemInstance.Rare == -2)
                {
                    requestData.ClientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_EQUIP_DESTROYED_SP,
                            requestData.ClientSession.Account.Language)
                    });
                    return;
                }
            }

            if (requestData.ClientSession.Character.JobLevel < itemInstance.Item.LevelJobMinimum)
            {
                requestData.ClientSession.SendPacket(
                    requestData.ClientSession.Character.GenerateSay(
                        requestData.ClientSession.GetMessageFromKey(LanguageKey.LOW_JOB_LVL),
                        Shared.Enumerations.SayColorType.Yellow));
                return;
            }

            requestData.ClientSession.Character.Inventory.MoveInPocket(packet.Slot, packet.Type, PocketType.Wear,
                (short) itemInstance.Item.EquipmentSlot, true);
            var newItem =
                requestData.ClientSession.Character.Inventory
                    .LoadBySlotAndType<IItemInstance>(packet.Slot, packet.Type);

            requestData.ClientSession.SendPacket(newItem.GeneratePocketChange(packet.Type, packet.Slot));

            requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character
                .GenerateEq());
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateEquipment());

            if (itemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSpPoint());
            }

            if (itemInstance.Item.EquipmentSlot == EquipmentType.Fairy)
            {
                requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(
                    requestData.ClientSession.Character.GeneratePairy(itemInstance as WearableInstance));
            }

            if (itemInstance.Item.EquipmentSlot == EquipmentType.Amulet)
            {
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateEff(39));
            }


            if (itemInstance.Item.ItemValidTime > 0 && itemInstance.BoundCharacterId != null)
            {
                itemInstance.ItemDeleteTime = SystemTime.Now().AddSeconds(itemInstance.Item.ItemValidTime);
            }

            if (itemInstance.Item.RequireBinding)
            {
                itemInstance.BoundCharacterId = requestData.ClientSession.Character.CharacterId;
            }
        }
    }
}