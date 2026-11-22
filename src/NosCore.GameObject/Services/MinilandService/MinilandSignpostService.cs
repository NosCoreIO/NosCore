//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.MinilandService;

public sealed class MinilandSignpostService(IMapInstanceAccessorService mapInstanceAccessorService,
    IMinilandRegistry minilandRegistry, IMapChangeService mapChangeService, IFriendHub friendHub, IClock clock)
    : IMinilandSignpostService
{
    // The client refuses signposts outside NosVille. It also has a line limiting them to villages, which no
    // map data marks, so only NosVille takes them here.
    public const short NosVilleMapId = 1;

    // Every signpost in the packet traces shows this dialog and faces this way.
    private const short Dialog = 10000;
    private const byte Direction = 2;

    public bool AllowsSignposts(MapInstance map) =>
        map is { MapInstanceType: MapInstanceType.BaseMapInstance, Map.MapId: NosVilleMapId };

    public Duration? TimeLeft(long ownerId)
    {
        return FindSignpost(ownerId) is { } signpost ? signpost.SignpostExpiresAt - clock.GetCurrentInstant() : null;
    }

    public async Task PlaceAsync(PlayerComponentBundle owner, NpcMonsterDto signpost)
    {
        var map = owner.MapInstance;
        var npc = map.PlaceNpc(signpost, owner.PositionX, owner.PositionY);
        npc.Dialog = Dialog;
        npc.Direction = Direction;
        npc.World.SetComponent(npc.Entity, new SignpostComponent(owner.CharacterId, owner.Name,
            clock.GetCurrentInstant() + SignpostSystem.Lifetime));
        await map.SendPacketAsync(npc.GenerateIn());
    }

    public async Task<bool> VisitAsync(ClientSession visitor, NpcComponentBundle npc)
    {
        var ownerId = npc.SignpostOwnerId;
        if (ownerId == 0)
        {
            return false;
        }

        if (minilandRegistry.GetByCharacterId(ownerId) is not { } miniland)
        {
            return true;
        }

        if (ownerId != visitor.Character.CharacterId)
        {
            switch (miniland.State)
            {
                case MinilandState.Lock:
                    await visitor.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.MinilandLocked });
                    return true;
                case MinilandState.Private when !await IsFriendAsync(ownerId, visitor.Character.CharacterId):
                    await visitor.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.MinilandIsPrivate });
                    return true;
            }
        }

        await mapChangeService.ChangeMapInstanceAsync(visitor, miniland.MapInstanceId, 5, 8);
        return true;
    }

    public async Task TakeDownAsync(long ownerId)
    {
        if (FindSignpost(ownerId) is { } signpost)
        {
            await signpost.TakeDownAsync();
        }
    }

    private NpcComponentBundle? FindSignpost(long ownerId)
    {
        var now = clock.GetCurrentInstant();
        return mapInstanceAccessorService.GetBaseMapById(NosVilleMapId)?
            .FindNpc(npc => npc.SignpostOwnerId == ownerId && npc.SignpostExpiresAt > now);
    }

    private async Task<bool> IsFriendAsync(long ownerId, long visitorId) =>
        (await friendHub.GetFriendsAsync(ownerId))
            .Any(friend => friend.CharacterId == visitorId && friend.RelationType != CharacterRelationType.Blocked);
}
