//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Networking;
using NosCore.Packets;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.LandOfDeathService
{
    // Every family gets its own land, made when its first member goes in and closed once its last
    // member has left. What happens inside is the script's; this keeps the gate and the way in.
    public class LandOfDeathService(
        IScriptedInstanceService scriptedInstanceService,
        IMapChangeService mapChangeService,
        ISessionRegistry sessionRegistry,
        IItemCatalog itemCatalog,
        IClock clock,
        ILogger<LandOfDeathService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : ILandOfDeathService, ISingletonService
    {
        public const string Script = "land-of-death";
        public const short EntranceX = 153;
        public const short EntranceY = 145;
        public const byte MinimumLevel = 55;

        // Until the first Flying Fire Devil has gone.
        public static readonly TimeSpan GateOpenFor = TimeSpan.FromMinutes(31);
        public static readonly TimeSpan ReviveTimeout = TimeSpan.FromSeconds(30);

        private readonly ConcurrentDictionary<long, ScriptedInstanceRun> _lands = new();
        private readonly SemaphoreSlim _landLock = new(1, 1);
        private IInstanceScript? _script;
        private Instant _openedAt;
        private int _gateOpen;

        public bool IsGateOpen => Volatile.Read(ref _gateOpen) == 1;

        public bool TryStart()
        {
            if (Interlocked.CompareExchange(ref _gateOpen, 1, 0) != 0)
            {
                return false;
            }

            _openedAt = clock.GetCurrentInstant();
            _ = Task.Run(async () =>
            {
                await Task.Delay(GateOpenFor);
                Volatile.Write(ref _gateOpen, 0);
            });
            return true;
        }

        public async Task<bool> CanEnterAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.Family == null)
            {
                await character.SendPacketAsync(Message(Game18NConstString.NeedFamilyForLandOfDeath));
                return false;
            }

            if (!IsGateOpen)
            {
                await character.SendPacketAsync(Message(Game18NConstString.NotAuthorised));
                return false;
            }

            if (character.Level < MinimumLevel)
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.TooLowLevelToEntry,
                    ArgumentType = 4,
                    Game18NArguments = { MinimumLevel },
                });
                return false;
            }

            return character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance && character.IsAlive;
        }

        public async Task EnterAsync(ClientSession session)
        {
            if (!await CanEnterAsync(session) || session.Character.Family is not { } family)
            {
                return;
            }

            if (await GetOrCreateLandAsync(family.FamilyId) is not { } land)
            {
                return;
            }

            await mapChangeService.ChangeMapInstanceAsync(session, land, EntranceX, EntranceY);
        }

        public async Task AskReviveAsync(PlayerComponentBundle player)
        {
            await player.SendPacketAsync(player.GenerateStat());
            await player.SendPacketAsync(new DlgiPacket
            {
                YesPacket = new RevivalPacket { Type = RevivalType.InPlace },
                NoPacket = new RevivalPacket { Type = RevivalType.AtSavePoint },
                Question = Game18NConstString.AskWantToUse,
                ArgumentType = 2,
                Game18NArguments = new Game18NArguments(2) { itemCatalog.GetMaterialVNum(MaterialType.Saver), 1 },
            });

            var mapInstanceId = player.MapInstance?.MapInstanceId;
            var askedAt = clock.GetCurrentInstant();
            player.ReviveAskedAt = askedAt;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ReviveTimeout);
                    if (player.ReviveAskedAt != askedAt || player.Hp > 0 || player.MapInstance?.MapInstanceId != mapInstanceId
                        || sessionRegistry.GetSessionByCharacterId(player.CharacterId) is not { } session)
                    {
                        return;
                    }

                    await ReturnToEntranceAsync(session);
                }
                catch (Exception e)
                {
                    logger.LogError(e, logLanguage[LogLanguageKey.LAND_OF_DEATH_REVIVE_FAILED], player.CharacterId);
                }
            });
        }

        public async Task ReturnToEntranceAsync(ClientSession session)
        {
            var player = session.Character;
            await ReviveWithOneHpAsync(player);
            await mapChangeService.ChangeMapAsync(session, player.MapId, player.MapX, player.MapY);
            await player.SendPacketAsync(Message(Game18NConstString.ReturnToEntranceLandOfDeath));
        }

        private static async Task ReviveWithOneHpAsync(PlayerComponentBundle player)
        {
            await player.OnMapAsync(() =>
            {
                player.Hp = 1;
                player.Mp = 1;
                player.IsAlive = true;
                return Task.CompletedTask;
            });
            if (player.MapInstance is { } mapInstance)
            {
                await mapInstance.SendPacketAsync(new RevivePacket
                {
                    VisualType = VisualType.Player,
                    VisualId = player.VisualId,
                    Data = 0,
                });
            }

            await player.SendPacketAsync(player.GenerateStat());
        }

        public Task LeaveAsync(ClientSession session) =>
            session.SendPacketAsync(new EventPacket { Type = EventPacketType.ClearTimeSpaceUI });

        private async Task<Guid?> GetOrCreateLandAsync(long familyId)
        {
            await _landLock.WaitAsync();
            try
            {
                if (_lands.TryGetValue(familyId, out var existing) && Land(existing) is { } open)
                {
                    return open;
                }

                _script ??= await scriptedInstanceService.LoadScriptAsync(Script);
                if (_script is not { } script)
                {
                    logger.LogError(logLanguage[LogLanguageKey.LAND_OF_DEATH_SCRIPT_MISSING], Script);
                    return null;
                }

                var run = await scriptedInstanceService.InstantiateAsync(script, MapInstanceType.LodInstance,
                    new LandHost(this, clock, _openedAt));
                if (run == null)
                {
                    return null;
                }

                _lands[familyId] = run;
                return Land(run);
            }
            finally
            {
                _landLock.Release();
            }
        }

        // A land that has ended, or whose last player has left, is closed; the family's next arrival opens a new one.
        private Guid? Land(ScriptedInstanceRun run)
        {
            var room = run.Rooms.Values.First();
            return run.EndState == null && scriptedInstanceService.GetRun(room) == run ? room : null;
        }

        private async Task ReturnAsync(ClientSession session)
        {
            var player = session.Character;
            if (!player.IsAlive)
            {
                await ReviveWithOneHpAsync(player);
            }

            await mapChangeService.ChangeMapAsync(session, player.MapId, player.MapX, player.MapY);
            await player.SendPacketAsync(Message(Game18NConstString.OutOfLandOfDeath));
        }

        private sealed class LandHost(LandOfDeathService service, IClock clock, Instant openedAt) : IInstanceRunHost
        {
            public string Name => Script;

            public TimeSpan AlreadyElapsed => (clock.GetCurrentInstant() - openedAt).ToTimeSpan();

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public Task EndedAsync(ScriptedInstanceRun run, EndStateType state) => Task.CompletedTask;

            public Task ReturnAsync(ClientSession session) => service.ReturnAsync(session);
        }

        private static MsgiPacket Message(Game18NConstString message) => new() { Type = MessageType.Default, Message = message };
    }
}
