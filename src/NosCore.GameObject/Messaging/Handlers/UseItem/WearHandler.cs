//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class WearHandler(
        ILogger logger,
        IClock clock,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        IOptions<WorldConfiguration> worldConfiguration)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var itemType = evt.InventoryItem.ItemInstance.Item.ItemType;
            if (itemType != ItemType.Weapon
                && itemType != ItemType.Jewelery
                && itemType != ItemType.Armor
                && itemType != ItemType.Fashion
                && itemType != ItemType.Specialist)
            {
                return;
            }

            var session = evt.ClientSession;
            var itemInstance = evt.InventoryItem;
            var packet = evt.Packet;

            await session.SendPacketAsync(session.Character.GenerateEff(123));

            if (session.Character.InExchangeOrShop)
            {
                logger.Error(logLanguage[LogLanguageKey.CANT_USE_ITEM_IN_SHOP]);
                return;
            }

            if (itemInstance.ItemInstance.BoundCharacterId == null && packet.Mode == 0 && itemInstance.ItemInstance.Item.RequireBinding)
            {
                await session.SendPacketAsync(new QnaPacket
                {
                    YesPacket = session.Character.GenerateUseItem(
                        (PocketType)itemInstance.Type,
                        itemInstance.Slot, 1, (byte)(packet.Parameter ?? 0)),
                    Question = session.GetMessageFromKey(LanguageKey.ASK_BIND)
                });
                return;
            }

            if (itemInstance.ItemInstance.Item.LevelMinimum > (itemInstance.ItemInstance.Item.IsHeroic
                    ? session.Character.HeroLevel : session.Character.Level)
                || (itemInstance.ItemInstance.Item.Sex != 0 &&
                    ((itemInstance.ItemInstance.Item.Sex >> (byte)session.Character.Gender) & 1) != 1)
                || (itemInstance.ItemInstance.Item.Class != 0 &&
                    ((itemInstance.ItemInstance.Item.Class >> (byte)session.Character.Class) & 1) != 1))
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotWearThat
                });
                return;
            }

            if (session.Character.UseSp && itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Fairy)
            {
                var sp = session.Character.InventoryService.LoadBySlotAndType(
                    (byte)EquipmentType.Sp, NoscorePocketType.Wear);

                if (sp != null && sp.ItemInstance.Item.Element != 0
                    && itemInstance.ItemInstance.Item.Element != sp.ItemInstance.Item.Element
                    && itemInstance.ItemInstance.Item.Element != sp.ItemInstance.Item.SecondaryElement)
                {
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.SpecialistAndFairyDifferentElement
                    });
                    return;
                }
            }

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                var timeSpanSinceLastSpUsage = clock.GetCurrentInstant().Minus(session.Character.LastSp).TotalSeconds;
                var sp = session.Character.InventoryService.LoadBySlotAndType(
                    (byte)EquipmentType.Sp, NoscorePocketType.Wear);
                if (timeSpanSinceLastSpUsage < session.Character.SpCooldown && sp != null)
                {
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.CantTrasformWithSideEffect,
                        ArgumentType = 4,
                        Game18NArguments = { session.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage) }
                    });
                    return;
                }

                if (session.Character.UseSp)
                {
                    await session.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = session.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.SpecialistCardsCannotBeTradedWhileTransformed
                    });
                    return;
                }

                if (itemInstance.ItemInstance.Rare == -2)
                {
                    await session.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.CantUseBecauseSoulDestroyed
                    });
                    return;
                }
            }

            if (session.Character.JobLevel < itemInstance.ItemInstance.Item.LevelJobMinimum)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CanNotBeWornDifferentClass
                });
                return;
            }

            session.Character.InventoryService.MoveInPocket(packet.Slot, (NoscorePocketType)packet.Type,
                NoscorePocketType.Wear, (short)itemInstance.ItemInstance.Item.EquipmentSlot, true);
            var newItem = session.Character.InventoryService
                .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.Type);

            await session.SendPacketAsync(newItem.GeneratePocketChange(packet.Type, packet.Slot));
            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateEq());
            await session.SendPacketAsync(session.Character.GenerateEquipment());

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                await session.SendPacketAsync(session.Character.GenerateSpPoint(worldConfiguration));
            }

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Fairy)
            {
                await session.Character.MapInstance.SendPacketAsync(
                    session.Character.GeneratePairy(itemInstance.ItemInstance as WearableInstance));
            }

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Amulet)
            {
                await session.SendPacketAsync(session.Character.GenerateEff(39));
            }

            itemInstance.ItemInstance.BoundCharacterId = session.Character.CharacterId;

            if (itemInstance.ItemInstance.Item.ItemValidTime > 0 && itemInstance.ItemInstance.BoundCharacterId != null)
            {
                itemInstance.ItemInstance.ItemDeleteTime =
                    clock.GetCurrentInstant().Plus(Duration.FromSeconds(itemInstance.ItemInstance.Item.ItemValidTime));
            }
        }
    }
}
