//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem;

[UsedImplicitly]
public sealed class MinilandSignpostHandler(IMinilandSignpostService signpostService, INpcMonsterCatalog npcMonsters)
{
    [UsedImplicitly]
    public async Task Handle(ItemUsedEvent evt)
    {
        var item = evt.InventoryItem.ItemInstance.Item;
        if (item.ItemType != ItemType.Special || item.Effect != ItemEffectType.MinilandSignpost
            || npcMonsters.GetNpcMonster(item.EffectValue) is not { } signpost)
        {
            return;
        }

        var session = evt.ClientSession;
        var character = session.Character;
        if (!signpostService.AllowsSignposts(character.MapInstance))
        {
            await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.SignPostOnlyNosville });
            return;
        }

        if (signpostService.TimeLeft(character.CharacterId) is { } left)
        {
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.AlreadyASignPost,
                ArgumentType = 4,
                Game18NArguments = { (long)Math.Ceiling(left.TotalSeconds) },
            });
            return;
        }

        await character.UseUpOneAsync(evt.InventoryItem);
        await signpostService.PlaceAsync(character, signpost);
    }
}
