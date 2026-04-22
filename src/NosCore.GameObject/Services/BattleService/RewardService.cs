//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Networking;
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
    IExperienceProgressionService experienceProgression,
    ILogger logger) : IRewardService
{
    // Flat fairy XP granted per qualifying kill. OpenNos exposes this as a runtime knob
    // on ServerManager; we keep it constant until there's a gameplay reason to tune it.
    private const int FairyXpRate = 1;
    // Gold item vnum. The GoldDropHandler already treats vnum 1046 as the "gold"
    // map item — we keep the same number so both systems stay in sync.
    private const short GoldVNum = 1046;

    public async Task DistributeAsync(IAliveEntity victim, IAliveEntity? killer, IReadOnlyDictionary<Entity, int> hitSnapshot)
    {
        if (victim is not INonPlayableEntity npc)
        {
            victim.HitList.Clear();
            return;
        }

        var mob = npc.NpcMonster;
        if (mob == null)
        {
            victim.HitList.Clear();
            return;
        }

        var mapInstance = victim.MapInstance;
        if (mapInstance == null)
        {
            victim.HitList.Clear();
            return;
        }

        var totalDamage = hitSnapshot.Values.Sum();
        if (totalDamage <= 0)
        {
            victim.HitList.Clear();
            return;
        }

        await AwardExperienceAsync(victim, mob, totalDamage, hitSnapshot);
        await SpawnDropsAsync(victim, mob, mapInstance);
        await SpawnGoldAsync(victim, mob, mapInstance);

        victim.HitList.Clear();
        _ = killer;
    }

    private async Task AwardExperienceAsync(IAliveEntity victim, NpcMonsterDto mob, int totalDamage, IReadOnlyDictionary<Entity, int> hitSnapshot)
    {
        foreach (var (handle, damage) in hitSnapshot)
        {
            if (!TryFindPlayer(victim, handle, out var player))
            {
                continue;
            }

            var share = damage / (double)totalDamage;
            var levelXp = (long)(mob.Xp * share);
            var jobXp = (long)(mob.JobXp * share);
            var heroXp = (long)(mob.HeroXp * share);

            // SP card XP uses the mob's JobXp, scaled by an SP-level multiplier that
            // front-loads early growth (OpenNos Character.cs:5230). We leave the
            // "is SP equipped / UseSp on" check to ExperienceProgressionService.
            var spXp = 0L;
            if (player.UseSp)
            {
                var spInstance = player.InventoryService
                    .LoadBySlotAndType((short)EquipmentType.Sp, NoscorePocketType.Wear)
                    ?.ItemInstance as SpecialistInstance;
                if (spInstance != null)
                {
                    var multiplier = spInstance.SpLevel < 10 ? 10 : spInstance.SpLevel < 19 ? 5 : 1;
                    spXp = (long)(mob.JobXp * share * multiplier);
                }
            }

            // Fairy XP is a flat per-kill grant gated on the mob being within ±15 levels
            // of the player (OpenNos Character.cs:5242). Out-of-bracket kills grant nothing,
            // so early-game farming doesn't power-level fairies on low-level mobs.
            var fairyXp = 0;
            if (Math.Abs(player.Level - mob.Level) <= 15)
            {
                fairyXp = FairyXpRate;
            }

            await experienceProgression.AddExperienceAsync(player, levelXp, jobXp, heroXp, spXp, fairyXp);
        }
    }

    private async Task SpawnDropsAsync(IAliveEntity victim, NpcMonsterDto mob, Services.MapInstanceGenerationService.MapInstance mapInstance)
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
                var dropped = mapItemGenerationService.Create(mapInstance, item, victim.PositionX, victim.PositionY);
                await mapInstance.SendPacketAsync(dropped.GenerateDrop());
            }
            catch (System.Exception ex)
            {
                logger.Warning(ex, "Failed to spawn drop {VNum} from {Mob}", drop.VNum, mob.NpcMonsterVNum);
            }
        }
    }

    private async Task SpawnGoldAsync(IAliveEntity victim, NpcMonsterDto mob, Services.MapInstanceGenerationService.MapInstance mapInstance)
    {
        // Mobs don't carry Gold as a column in this dataset; level-scaled amount gives a
        // predictable reward without needing extra content. Bumping this to a proper
        // GoldDrop field is easy once the content team provides one.
        var goldAmount = System.Math.Max(0, mob.Level * RandomHelper.Instance.RandomNumber(1, 5));
        if (goldAmount <= 0) return;

        try
        {
            var goldItem = itemGenerationService.Create(GoldVNum, (short)System.Math.Min(goldAmount, short.MaxValue));
            var dropped = mapItemGenerationService.Create(mapInstance, goldItem, victim.PositionX, victim.PositionY);
            await mapInstance.SendPacketAsync(dropped.GenerateDrop());
        }
        catch (System.Exception ex)
        {
            logger.Warning(ex, "Failed to spawn gold drop from {Mob}", mob.NpcMonsterVNum);
        }
    }

    // HitList uses Arch Entity handles, so we walk the same world back to the owning
    // player bundle. AddExperienceAsync is an extension on PlayerComponentBundle (not
    // ICharacterEntity) because it dispatches through strongly-typed bundle getters/setters
    // for level / LevelXp / JobLevelXp / HeroLevelXp.
    private static bool TryFindPlayer(IAliveEntity victim, Entity handle, out PlayerComponentBundle player)
    {
        if (victim.MapInstance is not null)
        {
            var world = victim.MapInstance.EcsWorld;
            if (world.World.IsAlive(handle))
            {
                var identity = world.TryGetComponent<Ecs.Components.EntityIdentityComponent>(handle);
                if (identity is { VisualType: Shared.Enumerations.VisualType.Player })
                {
                    player = new PlayerComponentBundle(handle, world);
                    return true;
                }
            }
        }
        player = default;
        return false;
    }
}
