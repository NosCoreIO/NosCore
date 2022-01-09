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

using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Networking;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.ItemGenerationService.Handlers
{
    public class WearEventHandler : IUseItemEventHandler
    {
        private readonly ILogger _logger;
        private readonly IClock _clock;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public WearEventHandler(ILogger logger, IClock clock, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _logger = logger;
            _clock = clock;
            _logLanguage = logLanguage;
        }

        public bool Condition(Item.Item item)
        {
            return (item.ItemType == ItemType.Weapon)
                || (item.ItemType == ItemType.Jewelery)
                || (item.ItemType == ItemType.Armor)
                || (item.ItemType == ItemType.Fashion)
                || (item.ItemType == ItemType.Specialist);
        }

        public async Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateEff(123)).ConfigureAwait(false);

            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;
            if (requestData.ClientSession.Character.InExchangeOrShop)
            {
                _logger.Error(_logLanguage[LogLanguageKey.CANT_USE_ITEM_IN_SHOP]);
                return;
            }

            if (itemInstance.ItemInstance!.BoundCharacterId == null && (packet.Mode == 0) && itemInstance.ItemInstance.Item!.RequireBinding)
            {
                await requestData.ClientSession.SendPacketAsync(
                    new QnaPacket
                    {
                        YesPacket = requestData.ClientSession.Character.GenerateUseItem(
                            (PocketType)itemInstance.Type,
                            itemInstance.Slot, 1, (byte)(packet.Parameter ?? 0)),
                        Question = requestData.ClientSession.GetMessageFromKey(LanguageKey.ASK_BIND)
                    }).ConfigureAwait(false);
                return;
            }

            if ((itemInstance.ItemInstance.Item!.LevelMinimum > (itemInstance.ItemInstance.Item.IsHeroic
                    ? requestData.ClientSession.Character.HeroLevel : requestData.ClientSession.Character.Level))
                || ((itemInstance.ItemInstance.Item.Sex != 0) &&
                    (((itemInstance.ItemInstance.Item.Sex >> (byte)requestData.ClientSession.Character.Gender) & 1) !=
                        1))
                || ((itemInstance.ItemInstance.Item.Class != 0) &&
                    (((itemInstance.ItemInstance.Item.Class >> (byte)requestData.ClientSession.Character.Class) & 1) !=
                        1)))
            {
                await requestData.ClientSession.SendPacketAsync(
                    requestData.ClientSession.Character.GenerateSay(
                        requestData.ClientSession.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT),
                        SayColorType.Yellow)).ConfigureAwait(false);
                return;
            }

            if (requestData.ClientSession.Character.UseSp &&
                (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Fairy))
            {
                var sp = requestData.ClientSession.Character.InventoryService.LoadBySlotAndType(
                    (byte)EquipmentType.Sp, NoscorePocketType.Wear);

                if ((sp != null) && (sp.ItemInstance!.Item!.Element != 0) &&
                    (itemInstance.ItemInstance.Item.Element != sp.ItemInstance.Item.Element) &&
                    (itemInstance.ItemInstance.Item.Element != sp.ItemInstance.Item.SecondaryElement))
                {
                    await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.SpecialistAndFairyDifferentElement
                    }).ConfigureAwait(false);
                    return;
                }
            }

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                var timeSpanSinceLastSpUsage =
                    (_clock.GetCurrentInstant().Minus(requestData.ClientSession.Character.LastSp)).TotalSeconds;
                var sp = requestData.ClientSession.Character.InventoryService.LoadBySlotAndType(
                    (byte)EquipmentType.Sp, NoscorePocketType.Wear);
                if ((timeSpanSinceLastSpUsage < requestData.ClientSession.Character.SpCooldown) && (sp != null))
                {
                    await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.CantTrasformWithSideEffect,
                        ArgumentType = 4,
                        Game18NArguments = new object[] { requestData.ClientSession.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage) }
                    }).ConfigureAwait(false);
                    return;
                }

                if (requestData.ClientSession.Character.UseSp)
                {
                    await requestData.ClientSession.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = requestData.ClientSession.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.SpecialistCardsCannotBeTradedWhileTransformed
                    }).ConfigureAwait(false);
                    return;
                }

                if (itemInstance.ItemInstance.Rare == -2)
                {
                    await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.CantUseBecauseSoulDestroyed
                    }).ConfigureAwait(false);
                    return;
                }
            }

            if (requestData.ClientSession.Character.JobLevel < itemInstance.ItemInstance.Item.LevelJobMinimum)
            {
                await requestData.ClientSession.SendPacketAsync(
                    requestData.ClientSession.Character.GenerateSay(
                        requestData.ClientSession.GetMessageFromKey(LanguageKey.LOW_JOB_LVL),
                        SayColorType.Yellow)).ConfigureAwait(false);
                return;
            }

            requestData.ClientSession.Character.InventoryService.MoveInPocket(packet.Slot, (NoscorePocketType)packet.Type,
                NoscorePocketType.Wear,
                (short)itemInstance.ItemInstance.Item.EquipmentSlot, true);
            var newItem =
                requestData.ClientSession.Character.InventoryService
                    .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.Type);

            await requestData.ClientSession.SendPacketAsync(newItem.GeneratePocketChange(packet.Type, packet.Slot)).ConfigureAwait(false);

            await requestData.ClientSession.Character.MapInstance.SendPacketAsync(requestData.ClientSession.Character
                .GenerateEq()).ConfigureAwait(false);
            await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateEquipment()).ConfigureAwait(false);

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateSpPoint()).ConfigureAwait(false);
            }

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Fairy)
            {
                await requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                    requestData.ClientSession.Character.GeneratePairy(itemInstance.ItemInstance as WearableInstance)).ConfigureAwait(false);
            }

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Amulet)
            {
                await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateEff(39)).ConfigureAwait(false);
            }

            itemInstance.ItemInstance.BoundCharacterId = requestData.ClientSession.Character.CharacterId;

            if ((itemInstance.ItemInstance.Item.ItemValidTime > 0) &&
                (itemInstance.ItemInstance.BoundCharacterId != null))
            {
                itemInstance.ItemInstance.ItemDeleteTime =
                    _clock.GetCurrentInstant().Plus(Duration.FromSeconds(itemInstance.ItemInstance.Item.ItemValidTime));
            }
        }
    }
}