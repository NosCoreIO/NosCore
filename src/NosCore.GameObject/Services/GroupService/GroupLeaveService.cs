//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.GroupService;

public sealed class GroupLeaveService(IIdService<Group> groupIdService, ISessionRegistry sessionRegistry,
    ISessionGroupFactory sessionGroupFactory, ICoupleBuffService coupleBuffService) : IGroupLeaveService, ISingletonService
{
    public async Task LeaveAsync(PlayerComponentBundle character)
    {
        var group = character.Group;
        if (group.Count == 1)
        {
            return;
        }

        var formerMembers = group.Values.Select(s => s.Item2).OfType<PlayerComponentBundle>().ToList();
        if (group.Count > 2)
        {
            await LeaveLargeGroupAsync(group, character);
        }
        else
        {
            await DisbandAsync(group);
        }

        await coupleBuffService.RefreshAsync(formerMembers);
    }

    private async Task LeaveLargeGroupAsync(Group group, PlayerComponentBundle character)
    {
        var isLeader = group.IsGroupLeader(character.CharacterId);
        await character.LeaveGroupAsync(sessionGroupFactory, sessionRegistry);

        if (isLeader)
        {
            var next = PlayersOf(group).FirstOrDefault();
            if (next == default || !sessionRegistry.TryGetCharacter(s => s.VisualId == next, out var newLeader))
            {
                return;
            }

            await newLeader.SendPacketAsync(new InfoiPacket
            {
                Message = Game18NConstString.YouAreNowGroupLeader
            });
        }

        if (group.Type != GroupType.Group)
        {
            return;
        }

        foreach (var memberId in PlayersOf(group))
        {
            if (sessionRegistry.TryGetCharacter(s => s.VisualId == memberId, out var member))
            {
                await member.SendPacketAsync(member.GeneratePinit());
            }
        }

        await character.SendPacketAsync(character.GeneratePinit());
        await character.MapInstance.SendPacketAsync(character.Group.GeneratePidx(character));
    }

    // The ids are read from the group's keys: a member that is leaving the world at the same
    // moment has no entity left to ask.
    private static List<long> PlayersOf(Group group) => group
        .Where(member => member.Key.Item1 == VisualType.Player)
        .OrderBy(member => member.Value.Item1)
        .Select(member => member.Key.Item2)
        .ToList();

    private async Task DisbandAsync(Group group)
    {
        foreach (var memberId in PlayersOf(group))
        {
            if (!sessionRegistry.TryGetCharacter(s => s.VisualId == memberId, out var target))
            {
                continue;
            }

            await target.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.PartyDisbanded
            });

            await target.LeaveGroupAsync(sessionGroupFactory, sessionRegistry);
            await target.SendPacketAsync(target.GeneratePinit());
            await target.MapInstance.SendPacketAsync(target.Group.GeneratePidx(target));
        }

        groupIdService.Items.TryRemove(group.GroupId, out _);
    }
}
