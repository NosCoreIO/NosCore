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
            if (!sessionRegistry.TryGetCharacter(s => s.VisualId == group.Values.First().Item2.VisualId, out var newLeader))
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

        foreach (var member in group.Values.Select(s => s.Item2).OfType<PlayerComponentBundle>())
        {
            await member.SendPacketAsync(member.GeneratePinit());
        }

        await character.SendPacketAsync(character.GeneratePinit());
        await character.MapInstance.SendPacketAsync(character.Group.GeneratePidx(character));
    }

    private async Task DisbandAsync(Group group)
    {
        var members = new List<INamedEntity>(group.Values.Select(s => s.Item2));
        foreach (var member in members.OfType<PlayerComponentBundle>())
        {
            if (!sessionRegistry.TryGetCharacter(s => s.VisualId == member.VisualId, out var target))
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
