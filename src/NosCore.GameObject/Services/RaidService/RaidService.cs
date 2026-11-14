//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.RaidService
{
    public class RaidService(IScriptedInstanceService scriptedInstanceService, IMapChangeService mapChangeService,
            ISessionRegistry sessionRegistry, ISessionGroupFactory sessionGroupFactory, IIdService<Group> groupIdService,
            IItemGenerationService itemProvider, IClock clock, ILogger<RaidService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : IRaidService
    {
        private const byte InTeam = 1;
        private const byte Leader = 2;
        private const long NoLeader = -1;
        private const int RevivesPerRun = 2;
        private static readonly Duration ReviveDelay = Duration.FromSeconds(20);

        public async Task OpenAsync(ClientSession leader, InventoryItemInstance seal)
        {
            var character = leader.Character;
            if (character.Group.Raid != null)
            {
                await MessageAsync(leader, Game18NConstString.AlreadyInRaid);
                return;
            }

            if (character.Group.Count > 1)
            {
                await MessageAsync(leader, Game18NConstString.CannotCreateRaidWhileInGroup);
                return;
            }

            var raid = await scriptedInstanceService.GetRaidBySealAsync(seal.ItemInstance.ItemVNum);
            if (raid == null)
            {
                await MessageAsync(leader, Game18NConstString.RaidDoesNotExist);
                return;
            }

            if (!character.InventoryService.TryConsume(seal, 1))
            {
                return;
            }

            await leader.SendPacketAsync(character.InventoryService.LoadBySlotAndType(seal.Slot, seal.Type)
                .GeneratePocketChange((PocketType)seal.Type, seal.Slot));

            using (await Group.MembershipLock.AcquireAsync())
            {
                character.Group.LeaveGroup(character);
                var team = new Group(GroupType.Team, sessionGroupFactory)
                {
                    GroupId = groupIdService.GetNextId(),
                    Raid = raid
                };
                groupIdService.Items[team.GroupId] = team;
                character.JoinGroup(team);
            }

            await MessageAsync(leader, Game18NConstString.YouAreRaidLeader);
            await leader.SendPacketsAsync(TeamPackets(character.CharacterId));
        }

        public async Task InviteAsync(ClientSession leader, long characterId)
        {
            var team = leader.Character.Group;
            if (team.Raid == null || team.Leader?.VisualId != leader.Character.CharacterId
                || characterId == leader.Character.CharacterId)
            {
                return;
            }

            var target = sessionRegistry.GetSessionByCharacterId(characterId);
            if (target is not { HasPlayerEntity: true })
            {
                return;
            }

            var refusal = JoinRefusal(team, target.Character.Group);
            if (refusal != null)
            {
                await MessageAsync(leader, refusal.Value);
                return;
            }

            leader.Character.GroupRequestCharacterIds.TryAdd(characterId, characterId);
            await target.SendPacketAsync(new Qnai2Packet
            {
                YesPacket = new RdPacket { Type = (short)RdRequest.Join, CharacterId = leader.Character.CharacterId, Parameter = 1 },
                Question = Game18NConstString.AskJoinToTeam,
                ArgumentType = 1,
                Game18NArguments = { leader.Character.Name }
            });
        }

        public async Task JoinAsync(ClientSession joiner, long leaderId)
        {
            var leader = sessionRegistry.GetSessionByCharacterId(leaderId);
            if (leader is not { HasPlayerEntity: true })
            {
                return;
            }

            var character = joiner.Character;
            Group team;
            Game18NConstString? refusal;
            using (await Group.MembershipLock.AcquireAsync())
            {
                if (!leader.Character.GroupRequestCharacterIds.TryRemove(character.CharacterId, out _))
                {
                    return;
                }

                team = leader.Character.Group;
                refusal = team.Raid == null ? Game18NConstString.RaidDoesNotExist : JoinRefusal(team, character.Group);
                if (refusal == null)
                {
                    character.Group.LeaveGroup(character);
                    character.JoinGroup(team);
                }
            }

            if (refusal != null)
            {
                await MessageAsync(joiner, refusal.Value);
                return;
            }

            await MessageAsync(joiner, Game18NConstString.JoinedToTeam);
            await joiner.SendPacketsAsync(TeamPackets(team.Leader?.VisualId ?? leaderId));
            await TellTeamAsync(team, character.CharacterId, Game18NConstString.PlayerJoinedToTeam, character.Name);
        }

        public async Task LeaveAsync(ClientSession member)
        {
            if (member.Character.Group.Raid == null)
            {
                return;
            }

            await RemoveAsync(member, Game18NConstString.PlayerHasLeftTeam, true);
        }

        public async Task KickAsync(ClientSession leader, long characterId)
        {
            var team = leader.Character.Group;
            if (team.Raid == null || team.RaidRun != null || team.Leader?.VisualId != leader.Character.CharacterId
                || characterId == leader.Character.CharacterId || !team.IsMember(characterId))
            {
                return;
            }

            var target = sessionRegistry.GetSessionByCharacterId(characterId);
            if (target is not { HasPlayerEntity: true })
            {
                return;
            }

            await MessageAsync(target, Game18NConstString.YouKickedFromTeam);
            await RemoveAsync(target, Game18NConstString.PlayerKickedFromTeam, true);
        }

        public async Task DisbandAsync(ClientSession leader)
        {
            var team = leader.Character.Group;
            if (team.Raid == null || team.RaidRun != null || team.Leader?.VisualId != leader.Character.CharacterId)
            {
                return;
            }

            await DissolveAsync(team, Game18NConstString.TeamDisbanded);
        }

        public async Task StartAsync(ClientSession leader)
        {
            var team = leader.Character.Group;
            var raid = team.Raid;
            if (raid?.Definition is not { } definition || team.Leader?.VisualId != leader.Character.CharacterId)
            {
                return;
            }

            if (team.RaidRun != null)
            {
                await MessageAsync(leader, Game18NConstString.RaidAlreadyStarted);
                return;
            }

            var members = Sessions(team).ToList();
            if (members.Count < definition.TeamSizeMinimum)
            {
                await MessageAsync(leader, Game18NConstString.TooFewRaidMember);
                return;
            }

            var leaderMap = leader.Character.MapInstance.MapInstanceId;
            if (members.Count != team.Count || members.Any(s => s.Character.MapInstance.MapInstanceId != leaderMap))
            {
                await MessageAsync(leader, Game18NConstString.AllRaidMembersShouldBeOn);
                return;
            }

            if (members.Any(s => s.Character.Level < raid.EffectiveLevelMinimum || s.Character.Level > raid.EffectiveLevelMaximum))
            {
                await MessageAsync(leader, Game18NConstString.LevelDoesNotMatch);
                return;
            }

            var run = raid.Logic is { } logic
                ? await scriptedInstanceService.InstantiateAsync(logic, MapInstanceType.RaidInstance, new RaidHost(this, team, raid))
                : null;
            if (run == null)
            {
                await MessageAsync(leader, Game18NConstString.RaidDoesNotExist);
                return;
            }

            team.RaidRun = run;

            var firstRoom = run.Rooms[definition.Rooms[0].Key];
            foreach (var member in members)
            {
                await mapChangeService.ChangeMapInstanceAsync(member, firstRoom, definition.StartX, definition.StartY);
            }

            await run.StartAsync();
        }

        public Task DisconnectAsync(ClientSession member) =>
            member.Character.Group.Raid != null
                ? RemoveAsync(member, Game18NConstString.PlayerHasLeftTeam, false)
                : Task.CompletedTask;

        private async Task EndedAsync(ScriptedInstanceRun run, Group team, ScriptedInstance raid, EndStateType state)
        {
            if (state == EndStateType.Success)
            {
                foreach (var member in run.Players().Where(s => s.HasPlayerEntity).ToList())
                {
                    await RewardAsync(member, raid);
                }
            }

            await DissolveAsync(team, null);
        }

        // Each member is brought back where they fell twice; a third fall takes them out of the team.
        private async Task FellAsync(RaidHost host, ScriptedInstanceRun run, PlayerComponentBundle player)
        {
            var falls = host.Falls.GetValueOrDefault(player.CharacterId) + 1;
            host.Falls[player.CharacterId] = falls;
            if (falls <= RevivesPerRun)
            {
                host.RevivesAt[player.CharacterId] = clock.GetCurrentInstant().Plus(ReviveDelay);
                await player.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = falls < RevivesPerRun
                        ? Game18NConstString.MessageRevivedInTwentySecondWithAnotherDeathAvailable
                        : Game18NConstString.MessageRevivedInTwentySecondWithNoMoreDeathsAvailable
                });
                return;
            }

            if (sessionRegistry.GetSessionByCharacterId(player.CharacterId) is not { HasPlayerEntity: true } session)
            {
                return;
            }

            run.StoodUp(player.CharacterId);
            await MessageAsync(session, Game18NConstString.FinishedLives);
            await RemoveAsync(session, Game18NConstString.PlayerHasLeftTeam, true);
        }

        private async Task ReviveDueAsync(RaidHost host, ScriptedInstanceRun run)
        {
            var now = clock.GetCurrentInstant();
            foreach (var (characterId, at) in host.RevivesAt.Where(s => s.Value <= now).ToList())
            {
                host.RevivesAt.Remove(characterId);
                if (run.Players().FirstOrDefault(s => s.HasPlayerEntity && s.Character.CharacterId == characterId) is { } session)
                {
                    run.StoodUp(characterId);
                    await session.Character.ReviveAsync(fully: true);
                }
            }
        }

        private async Task ReturnAsync(ClientSession session)
        {
            var player = session.Character;
            if (!player.IsAlive)
            {
                await player.ReviveAsync(fully: false);
            }

            await mapChangeService.ChangeMapAsync(session, player.MapId, player.MapX, player.MapY);
        }

        private async Task RewardAsync(ClientSession member, ScriptedInstance raid)
        {
            var character = member.Character;
            foreach (var gift in raid.Definition?.GiftItems ?? [])
            {
                var item = itemProvider.Create(gift.VNum, gift.Amount);
                var added = character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item, character.CharacterId))?.FirstOrDefault();
                if (added == null)
                {
                    logger.LogWarning(logLanguage[LogLanguageKey.RAID_REWARD_NO_ROOM],
                        raid.ScriptedInstanceId, gift.VNum, character.CharacterId);
                    await member.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                    continue;
                }

                await member.SendPacketAsync(added.GeneratePocketChange((PocketType)added.Type, added.Slot));
                await member.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                    2, gift.VNum.ToString(), gift.Amount));
            }
        }

        private async Task RemoveAsync(ClientSession member, Game18NConstString told, bool online)
        {
            var character = member.Character;
            var team = character.Group;
            var run = team.RaidRun;
            var wasLeader = team.Leader?.VisualId == character.CharacterId;
            using (await Group.MembershipLock.AcquireAsync())
            {
                if (!team.IsMember(character.CharacterId))
                {
                    return;
                }

                team.LeaveGroup(character);
                character.Group = new Group(GroupType.Group, sessionGroupFactory);
                character.Group.JoinGroup(character);
            }

            if (team.Count == 0)
            {
                groupIdService.Items.TryRemove(team.GroupId, out _);
            }

            if (online)
            {
                await member.SendPacketsAsync(LeftTeamPackets());
            }

            await TellTeamAsync(team, character.CharacterId, told, character.Name);
            if (wasLeader && team.Leader is { } newLeader)
            {
                await TellTeamAsync(team, null, Game18NConstString.PlayerBecameNewRaidLeader, newLeader.Name);
                await BroadcastAsync(team, new RaidPacket { Type = Leader, Value = newLeader.VisualId });
            }

            if (run == null)
            {
                return;
            }

            if (online && scriptedInstanceService.GetRun(character.MapInstance.MapInstanceId) == run)
            {
                await ReturnAsync(member);
            }
        }

        private async Task DissolveAsync(Group team, Game18NConstString? told)
        {
            List<ClientSession> members;
            using (await Group.MembershipLock.AcquireAsync())
            {
                members = Sessions(team).ToList();
                foreach (var member in members)
                {
                    team.LeaveGroup(member.Character);
                    member.Character.Group = new Group(GroupType.Group, sessionGroupFactory);
                    member.Character.Group.JoinGroup(member.Character);
                }

                team.Clear();
                groupIdService.Items.TryRemove(team.GroupId, out _);
            }

            foreach (var member in members)
            {
                if (told != null)
                {
                    await MessageAsync(member, told.Value);
                }

                await member.SendPacketsAsync(LeftTeamPackets());
            }
        }

        private IEnumerable<ClientSession> Sessions(Group team) =>
            team.Keys.Where(s => s.Item1 == VisualType.Player)
                .Select(s => sessionRegistry.GetSessionByCharacterId(s.Item2))
                .OfType<ClientSession>()
                .Where(s => s.HasPlayerEntity);

        private static Game18NConstString? JoinRefusal(Group team, Group joinerGroup)
        {
            if (team.RaidRun != null)
            {
                return Game18NConstString.RaidAlreadyStarted;
            }

            if (joinerGroup.Raid != null)
            {
                return Game18NConstString.AlreadyMemberOfAnotherTeam;
            }

            if (joinerGroup.Count > 1)
            {
                return Game18NConstString.PartyMembersNotAllowedToBeTeamMembers;
            }

            var maximum = team.Raid?.Definition?.TeamSizeMaximum is > 0 and var size ? size : (byte)team.Type;
            return team.Count >= maximum ? Game18NConstString.TeamIsFull : null;
        }

        private static IEnumerable<IPacket> TeamPackets(long leaderId) =>
        [
            new RaidPacket { Type = Leader, Value = leaderId },
            new RaidPacket { Type = InTeam, Value = 1 }
        ];

        private static IEnumerable<IPacket> LeftTeamPackets() =>
        [
            new RaidPacket { Type = InTeam, Value = 0 },
            new RaidPacket { Type = Leader, Value = NoLeader }
        ];

        private async Task TellTeamAsync(Group team, long? except, Game18NConstString message, string? name)
        {
            foreach (var member in Sessions(team).Where(s => s.Character.CharacterId != except))
            {
                await member.SendPacketAsync(new Msgi2Packet
                {
                    Type = MessageType.Default,
                    Message = message,
                    ArgumentType = 1,
                    Game18NArguments = { name ?? string.Empty }
                });
            }
        }

        private async Task BroadcastAsync(Group team, IPacket packet)
        {
            foreach (var member in Sessions(team))
            {
                await member.SendPacketAsync(packet);
            }
        }

        private static Task MessageAsync(ClientSession session, Game18NConstString message) =>
            session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });

        private sealed class RaidHost(RaidService service, Group team, ScriptedInstance raid) : IInstanceRunHost
        {
            public Dictionary<long, int> Falls { get; } = [];

            public Dictionary<long, Instant> RevivesAt { get; } = [];

            public string Name => $"raid {raid.ScriptedInstanceId}";

            public bool StartsOnFirstEntry => false;

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public Task EndedAsync(ScriptedInstanceRun run, EndStateType state) =>
                service.EndedAsync(run, team, raid, state);

            public Task ReturnAsync(ClientSession session) => service.ReturnAsync(session);

            public Task PlayerDownAsync(ScriptedInstanceRun run, PlayerComponentBundle player) =>
                service.FellAsync(this, run, player);

            public Task TickAsync(ScriptedInstanceRun run) => service.ReviveDueAsync(this, run);
        }
    }
}
