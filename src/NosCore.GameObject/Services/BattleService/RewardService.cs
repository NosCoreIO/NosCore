//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Helpers;
using Serilog;

namespace NosCore.GameObject.Services.BattleService;

// Reward distribution runs after a kill, exactly once. Monster XP is split among
// character attackers proportional to damage dealt — the "helper gets share" rule in the
// original game. Gold and drops are rolled once per kill and either awarded to the top
// damage dealer or spawned as map items everyone can pick up (we go with map items here,
// which matches the baseline behaviour and lets the MapItemPickedUpEvent system handle
// UI + grace periods).
public sealed class RewardService(
    IItemGenerationService itemGenerationService,
    IMapItemGenerationService mapItemGenerationService,
    INpcCombatCatalog catalog,
    ILogger logger) : IRewardService
{
    // Gold item vnum. The GoldDropHandler already treats vnum 1046 as the "gold"
    // map item — we keep the same number so both systems stay in sync.
    private const short GoldVNum = 1046;

    public Task DistributeAsync(IAliveEntity victim, IAliveEntity? killer)
    {
        if (victim is not INonPlayableEntity npc)
        {
            // Killing a player in PvP is handled elsewhere (dignity loss etc.).
            // We only distribute the monster-kill rewards here for now.
            victim.HitList.Clear();
            return Task.CompletedTask;
        }

        var mob = npc.NpcMonster;
        if (mob == null)
        {
            victim.HitList.Clear();
            return Task.CompletedTask;
        }

        var mapInstance = victim.MapInstance;
        if (mapInstance == null)
        {
            victim.HitList.Clear();
            return Task.CompletedTask;
        }

        var totalDamage = victim.HitList.Values.Sum();
        if (totalDamage <= 0)
        {
            victim.HitList.Clear();
            return Task.CompletedTask;
        }

        AwardExperience(victim, mob, totalDamage);
        SpawnDrops(victim, mob, mapInstance);
        SpawnGold(victim, mob, mapInstance);
        _ = ProgressKillQuestsAsync(victim, mob);

        victim.HitList.Clear();
        _ = killer;
        return Task.CompletedTask;
    }

    private static async Task ProgressKillQuestsAsync(IAliveEntity victim, NpcMonsterDto mob)
    {
        foreach (var (handle, _) in victim.HitList)
        {
            if (!TryFindCharacter(victim, handle, out var character))
            {
                continue;
            }

            var updated = false;
            foreach (var questKv in character.Quests)
            {
                var quest = questKv.Value;
                if (quest.CompletedOn != null)
                {
                    continue;
                }
                if (quest.Quest.QuestType != QuestType.Hunt && quest.Quest.QuestType != QuestType.NumberOfKill)
                {
                    continue;
                }
                foreach (var objective in quest.Quest.QuestObjectives)
                {
                    if (objective.FirstData != mob.NpcMonsterVNum)
                    {
                        continue;
                    }
                    var required = objective.SecondData ?? 0;
                    var current = quest.ObjectiveProgress.AddOrUpdate(objective.QuestObjectiveId, 1, (_, existing) => existing + 1);
                    if (current > required && required > 0)
                    {
                        quest.ObjectiveProgress[objective.QuestObjectiveId] = required;
                    }
                    updated = true;
                }
                if (updated)
                {
                    await character.SendPacketAsync(quest.GenerateQstiPacket(false));
                }
            }
        }
    }

    private static void AwardExperience(IAliveEntity victim, NpcMonsterDto mob, int totalDamage)
    {
        foreach (var (handle, damage) in victim.HitList)
        {
            if (!TryFindCharacter(victim, handle, out var character))
            {
                continue;
            }

            var share = damage / (double)totalDamage;
            character.LevelXp += (long)(mob.Xp * share);
            character.JobLevelXp += (long)(mob.JobXp * share);
            character.HeroXp += (long)(mob.HeroXp * share);
        }
    }

    private void SpawnDrops(IAliveEntity victim, NpcMonsterDto mob, Services.MapInstanceGenerationService.MapInstance mapInstance)
    {
        foreach (var drop in catalog.GetDrops(mob.NpcMonsterVNum))
        {
            // DropChance is expressed per 10000 (i.e. 2000 means 20%). Scale by the map's
            // drop rate so GM-configured rates apply uniformly.
            var roll = RandomHelper.Instance.RandomNumber(0, 10000);
            var threshold = drop.DropChance * mapInstance.DropRate;
            if (roll >= threshold) continue;

            try
            {
                var item = itemGenerationService.Create(drop.VNum, (short)drop.Amount);
                mapItemGenerationService.Create(mapInstance, item, victim.PositionX, victim.PositionY);
            }
            catch (System.Exception ex)
            {
                logger.Warning(ex, "Failed to spawn drop {VNum} from {Mob}", drop.VNum, mob.NpcMonsterVNum);
            }
        }
    }

    private void SpawnGold(IAliveEntity victim, NpcMonsterDto mob, Services.MapInstanceGenerationService.MapInstance mapInstance)
    {
        // Mobs don't carry Gold as a column in this dataset; level-scaled amount gives a
        // predictable reward without needing extra content. Bumping this to a proper
        // GoldDrop field is easy once the content team provides one.
        var goldAmount = System.Math.Max(0, mob.Level * RandomHelper.Instance.RandomNumber(1, 5));
        if (goldAmount <= 0) return;

        try
        {
            var goldItem = itemGenerationService.Create(GoldVNum, (short)System.Math.Min(goldAmount, short.MaxValue));
            mapItemGenerationService.Create(mapInstance, goldItem, victim.PositionX, victim.PositionY);
        }
        catch (System.Exception ex)
        {
            logger.Warning(ex, "Failed to spawn gold drop from {Mob}", mob.NpcMonsterVNum);
        }
    }

    // HitList uses Arch Entity handles, so we walk the same world back to the owning
    // character entity. Players are registered directly in the ECS via PlayerComponentBundle,
    // and we only care about ICharacterEntity for XP awards.
    private static bool TryFindCharacter(IAliveEntity victim, Entity handle, out ICharacterEntity character)
    {
        if (victim.MapInstance is not null)
        {
            var world = victim.MapInstance.EcsWorld;
            if (world.World.IsAlive(handle))
            {
                var identity = world.TryGetComponent<Ecs.Components.EntityIdentityComponent>(handle);
                if (identity is { VisualType: Shared.Enumerations.VisualType.Player })
                {
                    var player = new PlayerComponentBundle(handle, world);
                    character = player;
                    return true;
                }
            }
        }
        character = null!;
        return false;
    }
}
