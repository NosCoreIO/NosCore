//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using NodaTime;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Ecs;

public class MapWorld : IDisposable
{
    private readonly World _world;
    private readonly MovementSystem _movementSystem;
    private readonly MapInstance _mapInstance;
    private readonly ILogger _logger;
    private readonly IClock _clock;
    private CancellationTokenSource? _cts;
    private Task? _gameLoopTask;
    private bool _isRunning;

    public World World => _world;

    public MapWorld(MapInstance mapInstance, IHeuristic distanceCalculator, IClock clock, ILogger logger)
    {
        _world = World.Create();
        _movementSystem = new MovementSystem(distanceCalculator, clock);
        _mapInstance = mapInstance;
        _logger = logger;
        _clock = clock;
    }

    public ref T GetComponent<T>(Entity entity) where T : struct
    {
        return ref _world.Get<T>(entity);
    }

    public T? TryGetComponent<T>(Entity entity) where T : struct
    {
        if (!_world.IsAlive(entity))
        {
            return null;
        }
        return _world.Get<T>(entity);
    }

    public void SetComponent<T>(Entity entity, T component) where T : struct
    {
        if (_world.IsAlive(entity))
        {
            _world.Set(entity, component);
        }
    }

    public Entity CreateMonster(int monsterId, short vnum, short posX, short posY, byte direction,
        byte speed, bool isMoving, int hp, int maxHp, int mp, int maxMp, byte level, short race)
    {
        return _world.Create(
            new EntityIdentityComponent(monsterId, vnum, VisualType.Monster),
            new PositionComponent(posX, posY, direction, _mapInstance.MapInstanceId),
            new SpawnComponent(posX, posY),
            new NpcMovementComponent(speed, isMoving, false, _clock.GetCurrentInstant(), _clock.GetCurrentInstant()),
            new HealthComponent(hp, maxHp, true),
            new ManaComponent(mp, maxMp),
            new NpcDataComponent(level, race, true, false),
            new CombatComponent(false, 0),
            new VisualComponent(10, 0, 0, 0, 0, 0, 0, false)
        );
    }

    public Entity CreateNpc(int npcId, short vnum, short posX, short posY, byte direction,
        byte speed, bool isMoving, int hp, int maxHp, int mp, int maxMp, byte level, short race, short? dialog = null)
    {
        return _world.Create(
            new EntityIdentityComponent(npcId, vnum, VisualType.Npc),
            new PositionComponent(posX, posY, direction, _mapInstance.MapInstanceId),
            new SpawnComponent(posX, posY),
            new NpcMovementComponent(speed, isMoving, false, _clock.GetCurrentInstant(), _clock.GetCurrentInstant()),
            new HealthComponent(hp, maxHp, true),
            new ManaComponent(mp, maxMp),
            new NpcDataComponent(level, race, false, false, dialog),
            new CombatComponent(false, 0),
            new VisualComponent(10, 0, 0, 0, 0, 0, 0, false)
        );
    }

    public Entity CreateCharacter(long characterId, short posX, short posY, byte direction,
        byte speed, int hp, int maxHp, int mp, int maxMp, byte heroLevel, byte size,
        short morph, byte morphUpgrade, short morphDesign, byte morphBonus,
        string name, string? prefix, long accountId, AuthorityType authority,
        byte level, long levelXp, byte jobLevel, long jobLevelXp, long heroXp,
        CharacterClassType characterClass, GenderType gender, HairStyleType hairStyle, HairColorType hairColor,
        long gold, long reput, short dignity,
        int spPoint, int spAdditionPoint, short compliment,
        short saveMapId, short saveMapX, short saveMapY,
        bool useSp = false, bool isVehicled = false, byte? vehicleSpeed = null, bool canFight = true)
    {
        return _world.Create(
            new EntityIdentityComponent(characterId, 0, VisualType.Player),
            new PositionComponent(posX, posY, direction, _mapInstance.MapInstanceId),
            new NpcMovementComponent(speed, false, false, _clock.GetCurrentInstant(), _clock.GetCurrentInstant()),
            new HealthComponent(hp, maxHp, true),
            new ManaComponent(mp, maxMp),
            new CombatComponent(false, heroLevel, canFight),
            new CombatStateComponent(new CombatState()),
            new VisualComponent(size, morph, morphUpgrade, morphDesign, morphBonus, 0, 0, false, false, false, useSp, isVehicled, vehicleSpeed),
            new NameComponent(name, prefix, accountId, authority),
            new ExperienceComponent(level, levelXp, jobLevel, jobLevelXp, heroXp),
            new AppearanceComponent(characterClass, gender, hairStyle, hairColor),
            new GoldComponent(gold),
            new ReputationComponent(reput, dignity),
            new SpComponent(spPoint, spAdditionPoint, compliment),
            new PlayerComponent(characterId, accountId, saveMapId, saveMapX, saveMapY),
            new TimingComponent(_clock.GetCurrentInstant(), _clock.GetCurrentInstant(), 0, null),
            new PlayerFlagsComponent(false, false, false)
        );
    }

    public Entity CreateMapItem(long visualId, short vnum, short posX, short posY, long? ownerId, short amount)
    {
        return _world.Create(
            new EntityIdentityComponent(visualId, vnum, VisualType.Object),
            new PositionComponent(posX, posY, 0, _mapInstance.MapInstanceId),
            new MapItemComponent(ownerId, _clock.GetCurrentInstant(), amount)
        );
    }

    public Entity CreatePortal(int portalId, short sourceX, short sourceY, short sourceMapId,
        short destinationX, short destinationY, short destinationMapId, PortalType type, bool isDisabled,
        Guid sourceMapInstanceId, Guid destinationMapInstanceId, long? ownerId = null)
    {
        return _world.Create(
            new PortalComponent(portalId, sourceX, sourceY, sourceMapId, destinationX, destinationY,
                destinationMapId, type, isDisabled, sourceMapInstanceId, destinationMapInstanceId, ownerId)
        );
    }

    public void DestroyEntity(Entity entity)
    {
        if (_world.IsAlive(entity))
        {
            _world.Destroy(entity);
        }
    }

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _cts = new CancellationTokenSource();
        _gameLoopTask = RunGameLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _cts?.Cancel();
    }

    private async Task RunGameLoopAsync(CancellationToken ct)
    {
        const int tickRateMs = 400;
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(tickRateMs));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct).ConfigureAwait(false);

                if (_mapInstance.IsSleeping)
                {
                    continue;
                }

                _movementSystem.Update(_world, _mapInstance);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        World.Destroy(_world);
        GC.SuppressFinalize(this);
    }
}
