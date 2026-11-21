//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BaseCampService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem;

// Takes its user to their side's finished base camp after the usual five seconds.
[UsedImplicitly]
public sealed class BaseCampTeleportHandler(IBaseCampService baseCampService, IMapChangeService mapChangeService, IClock clock)
{
    private const byte Leave = 1;

    [UsedImplicitly]
    public async Task Handle(ItemUsedEvent evt)
    {
        var item = evt.InventoryItem.ItemInstance.Item;
        if (item.Effect != ItemEffectType.Teleport || item.EffectValue != (short)TeleportEffectValue.BaseCamp)
        {
            return;
        }

        var session = evt.ClientSession;
        var character = session.Character;
        if (character.MapInstance is not { IsAct4: true, MapInstanceType: MapInstanceType.BaseMapInstance })
        {
            await TellAsync(session, Game18NConstString.OnlyGlacernon2);
            return;
        }

        if (baseCampService.FinishedCamp(character.Faction) is not { } camp)
        {
            await TellAsync(session, Game18NConstString.NoBaseCamp);
            return;
        }

        var itemInstance = evt.InventoryItem;
        if (evt.Packet.Mode != Leave)
        {
            await character.StartDelayAsync(new DelayPacket
            {
                Delay = 5000,
                Type = DelayPacketType.ItemInUse,
                Packet = character.GenerateUseItem(itemInstance.Type.ToPocketType(), itemInstance.Slot, Leave, 0),
            }, itemInstance.ItemInstanceId, clock.GetCurrentInstant());
            return;
        }

        if (!character.TryCompleteDelay(DelayPacketType.ItemInUse, itemInstance.ItemInstanceId, clock.GetCurrentInstant()))
        {
            return;
        }

        await character.UseUpOneAsync(itemInstance);
        await mapChangeService.ChangeMapInstanceAsync(session, camp.MapInstanceId, camp.X, camp.Y);
    }

    private static Task TellAsync(ClientSession session, Game18NConstString message) =>
        session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });
}
