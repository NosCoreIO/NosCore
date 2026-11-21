//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BaseCampService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem;

// A side's flag builds its base camp where its user stands, after asking before replacing the one it has.
[UsedImplicitly]
public sealed class BaseFlagHandler(IBaseCampService baseCampService)
{
    private const byte Confirmed = 1;

    [UsedImplicitly]
    public async Task Handle(ItemUsedEvent evt)
    {
        var item = evt.InventoryItem.ItemInstance.Item;
        FactionType faction;
        switch (item.Effect)
        {
            case ItemEffectType.AngelBaseFlag:
                faction = FactionType.Angel;
                break;
            case ItemEffectType.DemonBaseFlag:
                faction = FactionType.Demon;
                break;
            default:
                return;
        }

        var session = evt.ClientSession;
        var character = session.Character;
        if (character.InExchangeOrShop || character.Hp <= 0
            || character.InventoryService.LoadBySlotAndType(evt.InventoryItem.Slot, evt.InventoryItem.Type)?.ItemInstanceId
                != evt.InventoryItem.ItemInstanceId)
        {
            return;
        }

        if (character.MapInstance is not { IsAct4: true, MapInstanceType: MapInstanceType.BaseMapInstance })
        {
            await TellAsync(session, Game18NConstString.OnlyGlacernon2);
            return;
        }

        if (character.Faction != faction)
        {
            await TellAsync(session, Game18NConstString.OnlyYouCanCampFlag);
            return;
        }

        if (evt.Packet.Mode != Confirmed && baseCampService.HasCamp(faction))
        {
            await session.SendPacketAsync(new QnaiPacket
            {
                YesPacket = character.GenerateUseItem(evt.InventoryItem.Type.ToPocketType(), evt.InventoryItem.Slot, Confirmed, 0),
                Question = Game18NConstString.BuildCampAfterRemovingTheLast,
            });
            return;
        }

        await character.UseUpOneAsync(evt.InventoryItem);
        await baseCampService.BuildAsync(character, (short)item.EffectValue);
    }

    private static Task TellAsync(ClientSession session, Game18NConstString message) =>
        session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });
}
