//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Networking;
using NosCore.Shared.Enumerations;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Services.CollectService;

public sealed class CollectService(INpcCombatCatalog catalog, IItemGenerationService itemProvider, IClock clock,
    Wolverine.IMessageBus messageBus) : ICollectService
{
    public const int CollectDelay = 5000;

    public async Task<bool> StartAsync(PlayerComponentBundle character, NpcComponentBundle npc)
    {
        if (npc.NpcMonster is not { CanCollect: true } || !npc.IsAlive)
        {
            return false;
        }

        await character.StartDelayAsync(new DelayPacket
        {
            Delay = CollectDelay,
            Type = DelayPacketType.InUse,
            Packet = new ClientGuriPacket
            {
                Type = GuriPacketType.CollectItem,
                Argument = (int)npc.VisualId
            }
        }, npc.VisualId, clock.GetCurrentInstant());
        return true;
    }

    public async Task<bool> CompleteAsync(PlayerComponentBundle character, int mapNpcId)
    {
        if (character.MapInstance.GetNpcById(mapNpcId) is not { } npc
            || npc.NpcMonster is not { CanCollect: true } mapObject
            || !npc.IsAlive
            || !character.TryCompleteDelay(DelayPacketType.InUse, npc.VisualId, clock.GetCurrentInstant()))
        {
            return false;
        }

        var drop = catalog.GetDrops(mapObject.NpcMonsterVNum).FirstOrDefault();
        if (drop != null)
        {
            var amount = (short)(drop.Amount > 0 ? drop.Amount : 1);
            var item = itemProvider.Create(drop.VNum, amount);
            var added = character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, character.CharacterId))?.FirstOrDefault();
            if (added == null)
            {
                await character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                return false;
            }

            await character.SendPacketAsync(added.GeneratePocketChange(added.Type.ToPocketType(), added.Slot));
            await character.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                2, drop.VNum.ToString(), amount));
        }

        npc.IsAlive = false;
        await npc.MapInstance.SendPacketAsync(new OutPacket { VisualType = VisualType.Npc, VisualId = npc.VisualId });
        npc.MapInstance.ScheduleRespawn(npc, clock.GetCurrentInstant().Plus(RespawnTiming.For(mapObject)));
        await messageBus.PublishAsync(new MapEntityCollectedEvent(character, npc));
        return true;
    }
}
