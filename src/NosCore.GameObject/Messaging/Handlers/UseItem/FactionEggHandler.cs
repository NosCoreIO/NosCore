using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem;

[UsedImplicitly]
public sealed class FactionEggHandler
{
    /// <summary>
    /// Confirms individual faction changes, rechecks eligibility, and consumes one egg while resetting contribution points.
    /// </summary>
    [UsedImplicitly]
    public async Task Handle(ItemUsedEvent evt)
    {
        var item = evt.InventoryItem.ItemInstance.Item;
        if (item.Effect != ItemEffectType.ChangeFaction || item.EffectValue is not (1 or 2))
        {
            return;
        }

        var session = evt.ClientSession;
        var character = session.Character;
        if (character.InExchangeOrShop || character.Hp <= 0
            || character.InventoryService.LoadBySlotAndType(evt.InventoryItem.Slot, evt.InventoryItem.Type)?.ItemInstanceId
                != evt.InventoryItem.ItemInstanceId || evt.InventoryItem.ItemInstance.Amount <= 0)
        {
            return;
        }

        var faction = (FactionType)item.EffectValue;
        if (character.Family != null)
        {
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = faction == FactionType.Angel
                    ? Game18NConstString.FamilyMemberPlayerCanOnlyUseAngelEgg
                    : Game18NConstString.FamilyMemberPlayerCanOnlyUseDemonEgg
            });
            return;
        }

        if (character.CharacterDto.Faction == (byte)faction)
        {
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = faction == FactionType.Angel
                    ? Game18NConstString.AlreadyAngelFaction
                    : Game18NConstString.AlreadyDemonFaction
            });
            return;
        }

        if (evt.Packet.Mode == 0)
        {
            await session.SendPacketAsync(new QnaiPacket
            {
                YesPacket = character.GenerateUseItem((PocketType)evt.InventoryItem.Type, evt.InventoryItem.Slot, 1, 0),
                Question = faction == FactionType.Angel
                    ? Game18NConstString.MessageToChangeFactionToAngelWithPointsReset
                    : Game18NConstString.MessageToChangeFactionToDemonWithPointsReset
            });
            return;
        }

        if (evt.Packet.Mode != 1)
        {
            return;
        }

        if (!character.InventoryService.TryConsume(evt.InventoryItem, 1))
        {
            return;
        }

        character.CharacterDto.Faction = (byte)faction;
        character.CharacterDto.Act4Points = 0;
        await session.SendPacketAsync(evt.InventoryItem.GeneratePocketChange((PocketType)evt.InventoryItem.Type, evt.InventoryItem.Slot));
        await session.SendPacketAsync(new FsPacket { Faction = faction });
        await character.MapInstance.SendPacketAsync(character.GenerateIn(string.Empty));
    }
}
