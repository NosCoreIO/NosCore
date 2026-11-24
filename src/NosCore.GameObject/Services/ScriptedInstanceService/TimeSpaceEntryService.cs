//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class TimeSpaceEntryService(IScriptedInstanceService scriptedInstanceService,
            IMapChangeService mapChangeService, ISessionRegistry sessionRegistry,
            ITimeSpaceRecordService timeSpaceRecordService)
        : ITimeSpaceEntryService
    {
        private const int LoserModeClockScale = 2;

        // What the No of the join question sends back, so the window opens without asking again.
        private const long DeclinedToJoin = 1;

        public async Task ShowAsync(ClientSession session, ScriptedInstance entrance, bool offerToJoin = true)
        {
            if (!await scriptedInstanceService.LoadAsync(entrance))
            {
                return;
            }

            if (offerToJoin && PartyRun(session.Character, entrance) is { } partyRun)
            {
                await session.SendPacketAsync(new DlgiPacket
                {
                    YesPacket = new WreqPacket { Value = WreqPacketType.TeamTimeSpaceEntry, Param = partyRun.MemberId },
                    NoPacket = new WreqPacket { Value = WreqPacketType.DailyTeamTimeSpaceEntry, Param = DeclinedToJoin },
                    Question = Game18NConstString.WantJoinParty
                });
                return;
            }

            var loserMode = entrance.Definition is { } definition
                && session.Character.OffersLoserMode(definition.Id);
            var completed = entrance.TimeSpaceId is { } id
                && session.Character.CompletedTimeSpaces?.Contains(id) == true;
            var highScore = entrance.Definition is { } shown
                ? await timeSpaceRecordService.GetHighScoreAsync(shown.Id)
                : null;
            await session.SendPacketAsync(entrance.GenerateRbr(loserMode, completed,
                !session.Character.HasClearedRequiredTimeSpaces(entrance), highScore));
        }

        public async Task AskRecordChallengeAsync(ClientSession session, ScriptedInstance entrance)
        {
            if (!await scriptedInstanceService.LoadAsync(entrance) || entrance.Definition?.RecordCost is not { } cost)
            {
                return;
            }

            await session.SendPacketAsync(new QnaiPacket
            {
                YesPacket = new TreqPacket
                {
                    X = entrance.PositionX,
                    Y = entrance.PositionY,
                    StartPress = true,
                    RecordPress = true,
                    GoldAccepted = true
                },
                Question = Game18NConstString.GoldRequiredToChallengeRecord,
                ArgumentType = 4,
                Game18NArguments = { cost }
            });
        }

        public async Task EnterAsync(ClientSession session, ScriptedInstance entrance, bool loserMode = false,
            bool recordChallenge = false)
        {
            var character = session.Character;
            if (!await scriptedInstanceService.LoadAsync(entrance))
            {
                await RefuseAsync(session, Game18NConstString.TimespaceDoesNotExist);
                return;
            }

            if (loserMode && !(entrance.Definition is { } offered && character.OffersLoserMode(offered.Id)))
            {
                return;
            }

            if (character.Level < entrance.EffectiveLevelMinimum
                || character.Level > entrance.EffectiveLevelMaximum)
            {
                await RefuseAsync(session, Game18NConstString.LevelDoesNotMatch);
                return;
            }

            if (!character.HasClearedRequiredTimeSpaces(entrance))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.SomeTimeSpaceStonesNotDone
                });
                return;
            }

            var missing = character.MissingRequiredItem(entrance);
            if (missing != null)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.FollowingItemRequired,
                    2, missing.VNum.ToString()));
                return;
            }

            var recordCost = 0L;
            if (recordChallenge)
            {
                if (loserMode || entrance.Definition?.RecordCost is not { } cost)
                {
                    return;
                }

                if (character.Gold < cost)
                {
                    await RefuseAsync(session, Game18NConstString.NotEnoughGold);
                    return;
                }

                recordCost = cost;
            }

            var run = await scriptedInstanceService.InstantiateAsync(entrance);
            if (run == null || entrance.Definition is not { } definition)
            {
                await RefuseAsync(session, Game18NConstString.TimespaceDoesNotExist);
                return;
            }

            if (loserMode)
            {
                run.IsLoserMode = true;
                run.ClockScale = LoserModeClockScale;
            }

            if (recordCost > 0)
            {
                character.Gold -= recordCost;
                await session.SendPacketAsync(character.GenerateGold());
            }

            await character.TakeRequiredItemsAsync(entrance);

            var firstRoom = run.Rooms[definition.Rooms[0].Key];
            var start = run.StartCell();
            await mapChangeService.ChangeMapInstanceAsync(session, firstRoom, start.X, start.Y);
        }

        // Joining pays the entry like opening a run does, and lands in the run's first room.
        public async Task JoinAsync(ClientSession session, ScriptedInstance entrance, long memberId)
        {
            var character = session.Character;
            if (!await scriptedInstanceService.LoadAsync(entrance)
                || PartyRun(character, entrance) is not { } partyRun || partyRun.MemberId != memberId
                || entrance.Definition is not { Rooms: [var first, ..] } definition)
            {
                return;
            }

            if (character.Level < entrance.EffectiveLevelMinimum
                || character.Level > entrance.EffectiveLevelMaximum)
            {
                await RefuseAsync(session, Game18NConstString.LevelDoesNotMatch);
                return;
            }

            var missing = character.MissingRequiredItem(entrance);
            if (missing != null)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.FollowingItemRequired,
                    2, missing.VNum.ToString()));
                return;
            }

            await character.TakeRequiredItemsAsync(entrance);
            await mapChangeService.ChangeMapInstanceAsync(session, partyRun.Run.Rooms[first.Key],
                definition.StartX, definition.StartY);
        }

        // A party member's unfinished run of the same time-space, if one is going on.
        private (long MemberId, ScriptedInstanceRun Run)? PartyRun(PlayerComponentBundle character, ScriptedInstance entrance)
        {
            if (character.Group.Count <= 1 || entrance.Definition is not { } definition)
            {
                return null;
            }

            foreach (var memberId in character.Group.Keys
                         .Where(k => k.Item1 == VisualType.Player && k.Item2 != character.VisualId).Select(k => k.Item2))
            {
                if (sessionRegistry.TryGetCharacter(c => c.VisualId == memberId, out var member)
                    && member.MapInstance is { } room
                    && scriptedInstanceService.GetRun(room.MapInstanceId) is { Host: TimeSpaceRunHost host, EndState: null } run
                    && host.Entrance.Definition?.Id == definition.Id)
                {
                    return (memberId, run);
                }
            }

            return null;
        }

        public Task EnterWithStoneAsync(ClientSession session, InventoryItemInstance stone)
        {
            var character = session.Character;
            if (character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
            {
                return RefuseAsync(session, Game18NConstString.CanNotBeUsedHere);
            }

            var item = stone.ItemInstance.Item;
            return EnterAsync(session, new ScriptedInstance
            {
                MapId = character.MapId,
                PositionX = character.PositionX,
                PositionY = character.PositionY,
                Type = ScriptedInstanceType.TimeSpace,
                Script = $"timespace-{item.EffectValue:00}",
                StoneVNum = item.VNum
            });
        }

        private static Task RefuseAsync(ClientSession session, Game18NConstString message)
        {
            return session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Yellow, message));
        }
    }
}
