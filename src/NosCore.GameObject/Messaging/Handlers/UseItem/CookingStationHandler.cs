//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.UseItem;

[UsedImplicitly]
public sealed class CookingStationHandler(INpcMonsterCatalog npcMonsters, IClock clock)
{
    // Every kit's description gives three minutes; its data row has no duration.
    public static readonly Duration Lifetime = Duration.FromMinutes(3);

    [UsedImplicitly]
    public async Task Handle(ItemUsedEvent evt)
    {
        var item = evt.InventoryItem.ItemInstance.Item;
        if (item.ItemType != ItemType.Special || item.Effect != ItemEffectType.CookingStation
            || npcMonsters.GetNpcMonster(item.EffectValue) is not { } station)
        {
            return;
        }

        var character = evt.ClientSession.Character;
        await character.UseUpOneAsync(evt.InventoryItem);
        var map = character.MapInstance;
        var npc = map.PlaceNpc(station, character.PositionX, character.PositionY);
        npc.World.AddComponent(npc.Entity, new ExpiryComponent(clock.GetCurrentInstant() + Lifetime));
        await map.SendPacketAsync(npc.GenerateIn());
    }
}
