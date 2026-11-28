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
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
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
            IItemGenerationService itemProvider, IClock clock, Wolverine.IMessageBus messageBus, IGameEventDispatcher gameEvents, ILogger<RaidService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : IRaidService
    {
        private const byte InTeam = 1;
        private const byte Leader = 2;
        private const byte HudShown = 4;
        private const byte Inside = 5;
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

            var raid = await scriptedInstanceService.GetRaidBySealAsync(seal.ItemInstance.ItemVNum);
            if (raid == null)
            {
                await MessageAsync(leader, Game18NConstString.RaidDoesNotExist);
                return;
            }

            Game18NConstString? refusal = null;
            using (await Group.MembershipLock.AcquireAsync())
            {
                if (character.Group.Count > 1 || character.Group.Raid != null)
                {
                    refusal = character.Group.Raid != null ? Game18NConstString.AlreadyInRaid : Game18NConstString.CannotCreateRaidWhileInGroup;
                }
                else if (character.InventoryService.TryConsume(seal, 1))
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
                else
                {
                    return;
                }
            }

            if (refusal != null)
            {
                await MessageAsync(leader, refusal.Value);
                return;
            }

            await leader.SendPacketAsync(character.InventoryService.LoadBySlotAndType(seal.Slot, seal.Type)
                .GeneratePocketChange(seal.Type.ToPocketType(), seal.Slot));

            await MessageAsync(leader, Game18NConstString.YouAreRaidLeader);
            await leader.SendPacketsAsync(TeamPackets(raid, character.CharacterId));
            await RefreshAsync(character.Group);
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

            team.Invited.TryAdd(characterId, 0);
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

            await AddAsync(joiner, leader, team => team.Invited.TryRemove(joiner.Character.CharacterId, out _));
        }

        // A listed team takes anyone who asks from the board, without an invitation.
        public async Task JoinListedAsync(ClientSession joiner, string leaderName)
        {
            var leader = sessionRegistry.GetSession(s => s.HasPlayerEntity && s.Character.Name == leaderName);
            if (leader == null)
            {
                await MessageAsync(joiner, Game18NConstString.RaidDoesNotExist);
                return;
            }

            await AddAsync(joiner, leader, team => team.Listed);
        }

        private async Task AddAsync(ClientSession joiner, ClientSession leader, Func<Group, bool> admitted)
        {
            var character = joiner.Character;
            var leaderId = leader.Character.CharacterId;
            Group team;
            Game18NConstString? refusal;
            using (await Group.MembershipLock.AcquireAsync())
            {
                team = leader.Character.Group;
                if (team.Leader?.VisualId != leaderId || !admitted(team))
                {
                    return;
                }

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
            await joiner.SendPacketsAsync(TeamPackets(team.Raid, team.Leader?.VisualId ?? leaderId));
            await TellTeamAsync(team, character.CharacterId, Game18NConstString.PlayerJoinedToTeam, character.Name);
            await RefreshAsync(team);
        }

        public async Task LeaveAsync(ClientSession member)
        {
            if (member.Character.Group.Raid == null)
            {
                return;
            }

            await RemoveAsync(member, member.Character.Group, Game18NConstString.PlayerHasLeftTeam, true);
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

            if (await RemoveAsync(target, team, Game18NConstString.PlayerKickedFromTeam, true))
            {
                await MessageAsync(target, Game18NConstString.YouKickedFromTeam);
            }
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

        // The leader starts the raid from its gate: stepping on it asks whether to open the raid,
        // and the answer sends mkraid.
        public async Task<bool> UseGateAsync(ClientSession session)
        {
            var character = session.Character;
            if (Gate(character) is not { } gate)
            {
                return false;
            }

            var team = character.Group;
            if (team.Raid == null)
            {
                await MessageAsync(session, Game18NConstString.CreateTeamFirst);
            }
            else if (team.Raid.ScriptedInstanceId != gate.ScriptedInstanceId)
            {
                await MessageAsync(session, Game18NConstString.RaidDoNotMatchSeal);
            }
            else if (team.Leader?.VisualId != character.CharacterId)
            {
                await MessageAsync(session, Game18NConstString.LeaderMustLeadRaid);
            }
            else
            {
                await session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = new MkRaidPacket(),
                    Question = Game18NConstString.AskCreateRaidMap
                });
            }

            return true;
        }

        private ScriptedInstance? Gate(PlayerComponentBundle character) =>
            character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance
            && scriptedInstanceService.GetNear(character.MapInstance.Map.MapId, character.PositionX, character.PositionY) is
                { Type: ScriptedInstanceType.Raid } gate
                ? gate
                : null;

        public async Task StartAsync(ClientSession leader)
        {
            var team = leader.Character.Group;
            var raid = team.Raid;
            if (raid?.Definition is not { } definition || team.Leader?.VisualId != leader.Character.CharacterId)
            {
                return;
            }

            if (StartRefusal(team, leader, raid, definition) is { } early)
            {
                await MessageAsync(leader, early);
                return;
            }

            var host = new RaidHost(this, team, raid);
            var run = raid.Logic is { } logic
                ? await scriptedInstanceService.InstantiateAsync(logic, MapInstanceType.RaidInstance, host)
                : null;
            if (run == null)
            {
                await MessageAsync(leader, Game18NConstString.RaidDoesNotExist);
                return;
            }

            // The roster may have changed while the rooms were built, so it is checked again and
            // fixed in the same step that marks the team as started.
            List<ClientSession>? members = null;
            Game18NConstString? refusal;
            using (await Group.MembershipLock.AcquireAsync())
            {
                refusal = StartRefusal(team, leader, raid, definition);
                if (refusal == null)
                {
                    team.RaidRun = run;
                    members = Sessions(team).ToList();
                    host.Lives = (short)members.Count;
                }
            }

            if (members == null)
            {
                await scriptedInstanceService.DisposeIfEmptyAsync(run);
                await MessageAsync(leader, refusal ?? Game18NConstString.RaidDoesNotExist);
                return;
            }

            var firstRoom = run.Rooms[definition.Rooms[0].Key];
            foreach (var member in members)
            {
                // An Act 6 raid closes its team window before the member is moved; the others close it on arrival.
                if (definition.IsAct6)
                {
                    await member.SendPacketAsync(TeamState(raid, InTeam, 0));
                }

                await mapChangeService.ChangeMapInstanceAsync(member, firstRoom, definition.StartX, definition.StartY);
            }

            await run.StartAsync();
        }

        private Game18NConstString? StartRefusal(Group team, ClientSession leader, ScriptedInstance raid, ScriptedInstanceDefinition definition)
        {
            if (team.RaidRun != null)
            {
                return Game18NConstString.RaidAlreadyStarted;
            }

            if (!ReferenceEquals(leader.Character.Group, team) || team.Leader?.VisualId != leader.Character.CharacterId)
            {
                return Game18NConstString.RaidDoesNotExist;
            }

            if (Gate(leader.Character)?.ScriptedInstanceId != raid.ScriptedInstanceId)
            {
                return Game18NConstString.RaidDoNotMatchSeal;
            }

            var members = Sessions(team).ToList();
            if (members.Count < definition.TeamSizeMinimum)
            {
                return Game18NConstString.TooFewRaidMember;
            }

            var leaderMap = leader.Character.MapInstance.MapInstanceId;
            if (members.Count != team.Count || members.Any(s => s.Character.MapInstance.MapInstanceId != leaderMap))
            {
                return Game18NConstString.AllRaidMembersShouldBeOn;
            }

            return members.Any(s => s.Character.Level < raid.EffectiveLevelMinimum || s.Character.Level > raid.EffectiveLevelMaximum)
                ? Game18NConstString.LevelDoesNotMatch
                : null;
        }

        public Task DisconnectAsync(ClientSession member) =>
            member.Character.Group.Raid != null
                ? RemoveAsync(member, member.Character.Group, Game18NConstString.PlayerHasLeftTeam, false)
                : Task.CompletedTask;

        private async Task EndedAsync(ScriptedInstanceRun run, Group team, ScriptedInstance raid, EndStateType state)
        {
            if (state == EndStateType.Success)
            {
                var winners = run.Players().Where(s => s.HasPlayerEntity).ToList();
                var banner = new RaidbfPacket { Type = RaidBannerType.BossDefeated, Unknown = HudValue(raid) };
                foreach (var winner in winners)
                {
                    await winner.SendPacketAsync(banner);
                }

                var familyWinners = winners
                    .Select(s => s.Character.Family is { } family ? new RaidWinner(family.FamilyId, s.Character.CharacterId) : null)
                    .OfType<RaidWinner>()
                    .ToList();
                foreach (var member in winners)
                {
                    await RewardAsync(member, raid);
                    if (raid.Definition is { } definition)
                    {
                        await gameEvents.RaiseAsync(new ScriptedInstanceSucceededEvent(member.Character, raid.Type, definition.Id));
                    }
                }

                if (raid.Definition is { } won)
                {
                    await messageBus.PublishAsync(new RaidWonEvent(familyWinners, won.Id, raid.IsHeroic));
                }
            }
        }

        // Once the raid is over, each member leaves the team as they are sent back, and their HUD is cleared then.
        private async Task SendBackAsync(RaidHost host, ClientSession session)
        {
            var team = host.Team;
            if (team.RaidRun is not { EndState: not null } run || !await LeaveOverAsync(session, team))
            {
                await ReturnAsync(session);
                return;
            }

            await session.SendPacketsAsync(
            [
                TeamState(host.Raid, Leader, NoLeader),
                new EventPacket { Type = EventPacketType.ClearTimeSpaceUI, Parameter = -1, SecondParameter = -1 }
            ]);
            await ReturnAsync(session);
            if (team.Count > 0 && !run.Players().Any(s => s.HasPlayerEntity && team.IsMember(s.Character.CharacterId)))
            {
                await DissolveAsync(team, null);
            }
        }

        private async Task<bool> LeaveOverAsync(ClientSession member, Group team)
        {
            var character = member.Character;
            using (await Group.MembershipLock.AcquireAsync())
            {
                if (!ReferenceEquals(character.Group, team) || !team.IsMember(character.CharacterId))
                {
                    return false;
                }

                team.LeaveGroup(character);
                character.Group = new Group(GroupType.Group, sessionGroupFactory);
                character.Group.JoinGroup(character);
                if (team.Count == 0)
                {
                    team.Invited.Clear();
                    groupIdService.Items.TryRemove(team.GroupId, out _);
                }
            }

            return true;
        }

        // Each member is brought back where they fell twice; a third fall takes them out of the team.
        private async Task FellAsync(RaidHost host, ScriptedInstanceRun run, PlayerComponentBundle player)
        {
            var falls = host.Falls.GetValueOrDefault(player.CharacterId) + 1;
            host.Falls[player.CharacterId] = falls;
            await RefreshAsync(host.Team);
            foreach (var teammate in run.Players().Where(s => s.HasPlayerEntity))
            {
                if (RoomOf(run, teammate) is { } room)
                {
                    await teammate.SendPacketAsync(Counters(host, run, room));
                }
            }

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
            await RemoveAsync(session, host.Team, Game18NConstString.PlayerHasLeftTeam, true);
        }

        // The start banner and the team's HUD reach each member once, on their first room; the counters on every room.
        private async Task ArrivedAsync(RaidHost host, ScriptedInstanceRun run, ClientSession session, int room)
        {
            var first = host.Arrived.Add(session.Character.CharacterId);
            if (first)
            {
                await session.SendPacketAsync(new RaidbfPacket { Type = RaidBannerType.Started, Unknown = HudValue(host.Raid) });
            }

            await session.SendPacketAsync(Counters(host, run, room));
            if (!first)
            {
                return;
            }

            var raid = host.Raid;
            var packets = new List<IPacket>();
            if (raid.Definition?.IsAct6 != true)
            {
                packets.Add(TeamState(raid, InTeam, 0));
            }

            packets.Add(TeamWindow(host.Team));
            packets.Add(TeamState(raid, Inside, 0));
            packets.Add(TeamState(raid, HudShown, null));
            packets.AddRange(MemberStats(host.Team));
            await session.SendPacketsAsync(packets);
        }

        // Every member's HP and MP, to the members inside, about once a second until they are sent back.
        private async Task ShowMemberStatsAsync(RaidHost host, ScriptedInstanceRun run)
        {
            var inside = run.Players().Where(s => s.HasPlayerEntity && host.Team.IsMember(s.Character.CharacterId)).ToList();
            if (inside.Count == 0)
            {
                return;
            }

            var stats = MemberStats(host.Team).ToList();
            foreach (var session in inside)
            {
                await session.SendPacketsAsync(stats);
            }
        }

        private IEnumerable<IPacket> MemberStats(Group team)
        {
            var members = Sessions(team).Select(s => s.Character).ToList();
            if (team.Raid?.Definition?.IsAct6 != true)
            {
                return
                [
                    new RaidMemberStatsPacket
                    {
                        Members = members.Select(c => (RaidMemberStatSubPacket?)new RaidMemberStatSubPacket
                        {
                            CharacterId = c.CharacterId,
                            HpPercent = Percent(c.Hp, c.MaxHp),
                            MpPercent = Percent(c.Mp, c.MaxMp)
                        }).ToList()
                    }
                ];
            }

            return
            [
                new RaidfhpPacket { Type = 3, Members = members.Select(c => Member(c.CharacterId, Percent(c.Hp, c.MaxHp))).ToList() },
                new RaidfmpPacket { Type = 3, Members = members.Select(c => Member(c.CharacterId, Percent(c.Mp, c.MaxMp))).ToList() }
            ];

            static RaidfMemberSubPacket? Member(long id, byte percent) => new() { CharacterId = id, Percent = percent };
        }

        private static byte Percent(long value, long maximum) =>
            maximum > 0 ? (byte)Math.Clamp(value * 100 / maximum, 0, 100) : (byte)0;

        private async Task LockerChangedAsync(RaidHost host, ScriptedInstanceRun run, int room)
        {
            var counters = Counters(host, run, room);
            foreach (var session in run.Players().Where(s => s.HasPlayerEntity && RoomOf(run, s) == room))
            {
                await session.SendPacketAsync(counters);
            }
        }

        // The lives are the team's size when the raid started, less one for each fall since.
        private static RaidmbfPacket Counters(RaidHost host, ScriptedInstanceRun run, int room)
        {
            var (goal, left) = run.LockerOf(room);
            return new RaidmbfPacket
            {
                MonsterGoal = goal,
                MonstersLeft = left,
                LivesLeft = (short)Math.Max(0, host.Lives - host.Falls.Values.Sum()),
                Lives = host.Lives,
                Unknown = HudValue(host.Raid)
            };
        }

        private static byte HudValue(ScriptedInstance raid) => raid.Definition?.RaidHudValue ?? 0;

        private static int? RoomOf(ScriptedInstanceRun run, ClientSession session) =>
            run.Rooms.Where(r => r.Value == session.Character.MapInstance.MapInstanceId)
                .Select(r => (int?)r.Key)
                .FirstOrDefault();

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
                var item = itemProvider.Create(gift.VNum, gift.Amount, 0, 0, gift.Design);
                var added = character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(item, character.CharacterId));
                if (added is not { Count: > 0 })
                {
                    logger.LogWarning(logLanguage[LogLanguageKey.RAID_REWARD_NO_ROOM],
                        raid.ScriptedInstanceId, gift.VNum, character.CharacterId);
                    await member.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                    continue;
                }

                foreach (var slot in added)
                {
                    await member.SendPacketAsync(slot.GeneratePocketChange(slot.Type.ToPocketType(), slot.Slot));
                }

                await member.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                    2, gift.VNum.ToString(), gift.Amount));
            }
        }

        // Takes the member out of that team only, so a decision made before an await cannot reach
        // a team they have moved to since. False when they were no longer in it.
        private async Task<bool> RemoveAsync(ClientSession member, Group team, Game18NConstString told, bool online)
        {
            var character = member.Character;
            ScriptedInstanceRun? run;
            bool wasLeader;
            using (await Group.MembershipLock.AcquireAsync())
            {
                if (!ReferenceEquals(character.Group, team) || !team.IsMember(character.CharacterId))
                {
                    return false;
                }

                run = team.RaidRun;
                wasLeader = team.Leader?.VisualId == character.CharacterId;
                team.LeaveGroup(character);
                character.Group = new Group(GroupType.Group, sessionGroupFactory);
                character.Group.JoinGroup(character);
                if (wasLeader)
                {
                    team.Invited.Clear();
                }
            }

            if (team.Count == 0)
            {
                groupIdService.Items.TryRemove(team.GroupId, out _);
            }

            if (online)
            {
                await member.SendPacketsAsync(LeftTeamPackets(team.Raid));
            }

            await TellTeamAsync(team, character.CharacterId, told, character.Name);
            if (wasLeader && team.Leader is { } newLeader)
            {
                await TellTeamAsync(team, null, Game18NConstString.PlayerBecameNewRaidLeader, newLeader.Name);
                await BroadcastAsync(team, TeamState(team.Raid, Leader, newLeader.VisualId));
            }

            await RefreshAsync(team);

            if (run != null && online && scriptedInstanceService.GetRun(character.MapInstance.MapInstanceId) == run)
            {
                await ReturnAsync(member);
            }

            return true;
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
                team.Invited.Clear();
                team.Listed = false;
                groupIdService.Items.TryRemove(team.GroupId, out _);
            }

            foreach (var member in members)
            {
                if (told != null)
                {
                    await MessageAsync(member, told.Value);
                }

                await member.SendPacketsAsync(LeftTeamPackets(team.Raid));
            }
        }

        public async Task ShowBoardAsync(ClientSession session)
        {
            var team = session.Character.Group;
            var isLeader = team.Raid != null && team.Leader?.VisualId == session.Character.CharacterId;
            var mode = isLeader
                ? team.Listed ? RlPacketType.WantToRegister : RlPacketType.WantToUnregister
                : team.Raid != null ? RlPacketType.MemberAndAlreadyInList : RlPacketType.Normal;
            await session.SendPacketAsync(Board(mode));
        }

        public async Task ListAsync(ClientSession leader)
        {
            var team = leader.Character.Group;
            if (team.Raid == null || team.RaidRun != null || team.Leader?.VisualId != leader.Character.CharacterId)
            {
                return;
            }

            if (team.Listed)
            {
                await MessageAsync(leader, Game18NConstString.AlreadyRegistered2);
                return;
            }

            team.Listed = true;
            await leader.SendPacketAsync(Board(RlPacketType.WantToRegister));
        }

        public async Task UnlistAsync(ClientSession leader)
        {
            var team = leader.Character.Group;
            if (team.Raid == null || !team.Listed)
            {
                return;
            }

            if (team.Leader?.VisualId != leader.Character.CharacterId)
            {
                await MessageAsync(leader, Game18NConstString.OnlyLeaderCanDeleteRaid);
                return;
            }

            team.Listed = false;
            await leader.SendPacketAsync(Board(RlPacketType.WantToUnregister));
            await MessageAsync(leader, Game18NConstString.RemovedFromRaidList);
        }

        private ServerRlPacket Board(RlPacketType mode)
        {
            var teams = new List<ServerRlSubPacket?>();
            foreach (var team in groupIdService.Items.Values.Where(s => s is { Listed: true, Raid: not null, RaidRun: null }))
            {
                if (team.Leader is not { } head
                    || sessionRegistry.GetSessionByCharacterId(head.VisualId) is not { HasPlayerEntity: true } leader)
                {
                    continue;
                }

                teams.Add(new ServerRlSubPacket
                {
                    RaidId = team.Raid?.Definition?.Id ?? 0,
                    LevelMinimum = team.Raid?.EffectiveLevelMinimum ?? 0,
                    LevelMaximum = team.Raid?.EffectiveLevelMaximum ?? 0,
                    LeaderName = leader.Character.Name,
                    LeaderLevel = leader.Character.Level,
                    LeaderMorph = Morph(leader.Character),
                    LeaderClass = leader.Character.Class,
                    LeaderGender = leader.Character.Gender,
                    MemberCount = (short)team.Count,
                    LeaderHeroLevel = leader.Character.HeroLevel
                });
            }

            return new ServerRlPacket { Mode = (byte)mode, Teams = teams };
        }

        // Redraws the raid team window of every member.
        private Task RefreshAsync(Group team) => BroadcastAsync(team, TeamWindow(team));

        // The team window, with each member's falls in this run.
        private IPacket TeamWindow(Group team)
        {
            var falls = (team.RaidRun?.Host as RaidHost)?.Falls;
            var levelMinimum = team.Raid?.EffectiveLevelMinimum ?? 0;
            var levelMaximum = team.Raid?.EffectiveLevelMaximum ?? 0;
            var raidId = team.Raid?.Definition?.Id ?? 0;
            var members = Sessions(team).Select(s => (RdlstSubPacket?)new RdlstSubPacket
            {
                Level = s.Character.Level,
                Morph = Morph(s.Character),
                Class = s.Character.Class,
                Deaths = (short)(falls?.GetValueOrDefault(s.Character.CharacterId) ?? 0),
                Name = s.Character.Name,
                Gender = s.Character.Gender,
                CharacterId = s.Character.CharacterId,
                HeroLevel = s.Character.HeroLevel
            }).ToList();
            return team.Raid?.Definition?.IsAct6 == true
                ? new RdlstfPacket { LevelMinimum = levelMinimum, LevelMaximum = levelMaximum, RaidId = raidId, Members = members }
                : new RdlstPacket { LevelMinimum = levelMinimum, LevelMaximum = levelMaximum, RaidId = raidId, Members = members };
        }

        private static short Morph(PlayerComponentBundle player) =>
            player.UseSp || player.IsVehicled ? (short)player.Morph : (short)-1;

        private IEnumerable<ClientSession> Sessions(Group team) =>
            team.MembersInJoiningOrder.Where(s => s.VisualType == VisualType.Player)
                .Select(s => sessionRegistry.GetSessionByCharacterId(s.VisualId))
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

        private static IEnumerable<IPacket> TeamPackets(ScriptedInstance? raid, long leaderId) =>
        [
            TeamState(raid, Leader, leaderId),
            TeamState(raid, InTeam, 1)
        ];

        private static IEnumerable<IPacket> LeftTeamPackets(ScriptedInstance? raid) =>
        [
            TeamState(raid, InTeam, 0),
            TeamState(raid, Leader, NoLeader)
        ];

        // An Act 6 raid sends raidf where the others send raid.
        private static IPacket TeamState(ScriptedInstance? raid, byte type, long? value) =>
            raid?.Definition?.IsAct6 == true
                ? new RaidfPacket { Type = type, Value = value }
                : new RaidPacket { Type = type, Value = value };

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
            public Group Team => team;

            public ScriptedInstance Raid => raid;

            public short Lives { get; set; }

            public HashSet<long> Arrived { get; } = [];

            public Dictionary<long, int> Falls { get; } = [];

            public Dictionary<long, Instant> RevivesAt { get; } = [];

            public string Name => $"raid {raid.ScriptedInstanceId}";

            public bool StartsOnFirstEntry => false;

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public Task EndedAsync(ScriptedInstanceRun run, EndStateType state) =>
                service.EndedAsync(run, team, raid, state);

            public Task ReturnAsync(ClientSession session) => service.SendBackAsync(this, session);

            public Task PlayerDownAsync(ScriptedInstanceRun run, PlayerComponentBundle player) =>
                service.FellAsync(this, run, player);

            public Task TickAsync(ScriptedInstanceRun run) => service.ReviveDueAsync(this, run);

            public Task HudTickAsync(ScriptedInstanceRun run) => service.ShowMemberStatsAsync(this, run);

            public Task ArrivedAsync(ScriptedInstanceRun run, ClientSession session, int room) =>
                service.ArrivedAsync(this, run, session, room);

            public Task LockerChangedAsync(ScriptedInstanceRun run, int room) =>
                service.LockerChangedAsync(this, run, room);
        }
    }
}
