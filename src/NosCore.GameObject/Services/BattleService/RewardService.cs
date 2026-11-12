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
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Shared.Helpers;
using Microsoft.Extensions.Logging;
using NosCore.Algorithm.ExperienceShareService;
using NosCore.Algorithm.LevelPenaltyService;
using NosCore.Algorithm.LowLevelBoostService;

namespace NosCore.GameObject.Services.BattleService;

// Reward distribution runs after a kill, exactly once. Monster XP is split among
// character attackers proportional to damage dealt. Gold and drops are rolled once per kill
// and spawned as map items owned by the top damage dealer, or by whoever the group's looting
// mode hands them to.
public sealed class RewardService(
    IItemGenerationService itemGenerationService,
    IItemCatalog itemCatalog,
    IMapItemGenerationService mapItemGenerationService,
    INpcCombatCatalog catalog,
    IExperienceProgressionService experienceProgression,
    IExperienceShareService experienceShareService,
    ILevelPenaltyService levelPenaltyService,
    ILowLevelBoostService lowLevelBoostService,
    IGroupLootService groupLootService,
    IMateProgressionService mateProgression,
    IBuffService buffService,
    IEquipmentStatsService equipmentStatsService,
    ILogger<RewardService> logger) : IRewardService
{
    // Flat fairy XP granted per qualifying kill. OpenNos exposes this as a runtime knob
    // on ServerManager; we keep it constant until there's a gameplay reason to tune it.
    private const int FairyXpRate = 1;

    public async Task DistributeAsync(IAliveEntity victim, IAliveEntity? killer, IReadOnlyDictionary<Entity, int> hitSnapshot)
    {
        try
        {
            if (victim is not INonPlayableEntity npc)
            {
                return;
            }

            var mob = npc.NpcMonster;
            if (mob == null)
            {
                return;
            }

            var mapInstance = victim.MapInstance;
            if (mapInstance == null)
            {
                return;
            }

            var totalDamage = hitSnapshot.Values.Sum();
            if (totalDamage <= 0)
            {
                return;
            }

            // Kills are rewarded in parallel; each one's experience goes in on the map, so two
            // kills credited to one player at once neither lose experience nor level up twice.
            await mapInstance.Executor.RunAsync(() => AwardExperienceAsync(victim, mob, totalDamage, hitSnapshot));
            var topDamageDealer = FindTopDamageDealer(victim, hitSnapshot);
            await SpawnDropsAsync(victim, mob, mapInstance, topDamageDealer);
            await SpawnGoldAsync(victim, mob, mapInstance, topDamageDealer);
            _ = killer;
        }
        finally
        {
            victim.HitList.Clear();
        }
    }

    // A group splits the kill by its members' summed damage. Each member on the map receives
    // the group's part times their share of it, whether they hit the monster or not.
    private async Task AwardExperienceAsync(IAliveEntity victim, NpcMonsterDto mob, int totalDamage, IReadOnlyDictionary<Entity, int> hitSnapshot)
    {
        var parties = new Dictionary<long, (List<PlayerComponentBundle> Members, int Damage)>();
        foreach (var (handle, damage) in hitSnapshot)
        {
            if (!TryFindPlayer(victim, handle, out var player))
            {
                continue;
            }

            var key = player.Group.Count > 1 ? player.Group.GroupId : -player.CharacterId - 1;
            if (parties.TryGetValue(key, out var party))
            {
                parties[key] = (party.Members, party.Damage + damage);
                continue;
            }

            parties[key] = (MembersOnMap(player, victim), damage);
        }

        foreach (var (members, damage) in parties.Values.Where(p => p.Members.Count > 0))
        {
            var share = damage / (double)totalDamage;
            var groupSize = (byte)Math.Max(1, members[0].Group.Count);
            foreach (var member in members)
            {
                await AwardMemberExperienceAsync(member, mob,
                    share * experienceShareService.GetExperienceShare(groupSize),
                    share * experienceShareService.GetJobExperienceShare(groupSize));
            }
        }
    }

    private static List<PlayerComponentBundle> MembersOnMap(PlayerComponentBundle player, IAliveEntity victim)
    {
        if (player.Group.Count <= 1)
        {
            return [player];
        }

        return player.Group.Values
            .Select(s => s.Item2)
            .OfType<PlayerComponentBundle>()
            .Where(s => s.World == victim.MapInstance.EcsWorld && s.World.IsAlive(s.Handle))
            .ToList();
    }

    private async Task AwardMemberExperienceAsync(PlayerComponentBundle player, NpcMonsterDto mob, double share, double jobShare)
    {
        var buffBonus = 1 + ExperienceBuffPercent(player) / 100d;
        var levelXp = (long)(mob.Xp * share
            * levelPenaltyService.GetExperiencePenalty(player.Level, mob.Level)
            * lowLevelBoostService.GetExperienceBoost(player.Level)
            * buffBonus);
        var jobXp = (long)(mob.JobXp * jobShare * lowLevelBoostService.GetJobExperienceBoost(player.JobLevel) * buffBonus);
        var heroXp = (long)(mob.HeroXp * share);

        var spXp = 0L;
        if (player.UseSp)
        {
            var spInstance = player.InventoryService
                .LoadBySlotAndType((short)EquipmentType.Sp, NoscorePocketType.Wear)
                ?.ItemInstance as SpecialistInstance;
            if (spInstance != null)
            {
                var multiplier = spInstance.SpLevel < 10 ? 10 : spInstance.SpLevel < 19 ? 5 : 1;
                spXp = (long)(mob.JobXp * jobShare * multiplier);
                if (spInstance.SpLevel > 19)
                {
                    jobXp /= 2;
                }
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
        await mateProgression.AddExperienceAsync(player, levelXp);

        await AwardReputationAsync(player, mob).ConfigureAwait(false);
        await AwardDignityAsync(player, mob).ConfigureAwait(false);
    }

    private int ExperienceBuffPercent(PlayerComponentBundle player)
    {
        var percent = 0;
        foreach (var bCard in buffService.GetActiveBuffs(player).SelectMany(b => b.BCards))
        {
            switch (bCard.Effect())
            {
                case BCardEffect.ItemExpIncreased:
                    percent += BattleStatsProvider.ScaleByLevel(bCard, player.Level);
                    break;
                case BCardEffect.ItemExpIncreasedNegated:
                    percent -= BattleStatsProvider.ScaleByLevel(bCard, player.Level);
                    break;
            }
        }

        return percent;
    }

    private int GoldBonusPercent(PlayerComponentBundle player)
    {
        var percent = 0;
        var cards = buffService.GetActiveBuffs(player).SelectMany(b => b.BCards)
            .Concat(equipmentStatsService.Resolve(player).BCards ?? []);
        foreach (var bCard in cards)
        {
            switch (bCard.Effect())
            {
                case BCardEffect.ItemIncreaseEarnedGold:
                    percent += BattleStatsProvider.ScaleByLevel(bCard, player.Level);
                    break;
                case BCardEffect.ItemIncreaseEarnedGoldNegated:
                    percent -= BattleStatsProvider.ScaleByLevel(bCard, player.Level);
                    break;
            }
        }

        return percent;
    }

    private static async Task AwardReputationAsync(PlayerComponentBundle player, NpcMonsterDto mob)
    {
        if (player.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance || mob.Level < 3)
        {
            return;
        }
        player.Reput += mob.Level / 3;
        await player.SendPacketAsync(player.GenerateFd()).ConfigureAwait(false);
    }

    private static async Task AwardDignityAsync(PlayerComponentBundle player, NpcMonsterDto mob)
    {
        if (player.Level <= 20 || player.Level >= mob.Level || player.Dignity >= 100)
        {
            return;
        }
        if (RandomHelper.Instance.RandomNumber(0, 2) != 0)
        {
            return;
        }
        player.Dignity = (short)(player.Dignity + 1);
        await player.SendPacketAsync(player.GenerateFd()).ConfigureAwait(false);
    }

    private static PlayerComponentBundle? FindTopDamageDealer(IAliveEntity victim, IReadOnlyDictionary<Entity, int> hitSnapshot)
    {
        PlayerComponentBundle? topDamageDealer = null;
        var topDamage = 0;
        foreach (var (handle, damage) in hitSnapshot)
        {
            if (damage > topDamage && TryFindPlayer(victim, handle, out var player))
            {
                topDamage = damage;
                topDamageDealer = player;
            }
        }

        return topDamageDealer;
    }

    private long? NextOwner(IAliveEntity victim, PlayerComponentBundle? topDamageDealer) =>
        topDamageDealer is { } dealer ? groupLootService.AssignOwner(dealer, victim) : null;

    private async Task SpawnDropsAsync(IAliveEntity victim, NpcMonsterDto mob, Services.MapInstanceGenerationService.MapInstance mapInstance, PlayerComponentBundle? topDamageDealer)
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
                var dropped = mapItemGenerationService.Create(mapInstance, item, victim.PositionX, victim.PositionY,
                    NextOwner(victim, topDamageDealer));
                await mapInstance.SendPacketAsync(dropped.GenerateDrop());
            }
            catch (System.Exception ex)
            {
                logger.LogWarning(ex, "Failed to spawn drop {VNum} from {Mob}", drop.VNum, mob.NpcMonsterVNum);
            }
        }
    }

    private async Task SpawnGoldAsync(IAliveEntity victim, NpcMonsterDto mob, Services.MapInstanceGenerationService.MapInstance mapInstance, PlayerComponentBundle? topDamageDealer)
    {
        if (mapInstance.MapInstanceType != MapInstanceType.BaseMapInstance
            && mapInstance.MapInstanceType != MapInstanceType.TimeSpaceInstance)
        {
            return;
        }

        var goldAmount = RandomHelper.Instance.RandomNumber(6 * mob.Level, 12 * mob.Level);
        if (topDamageDealer is { } owner)
        {
            if (RandomHelper.Instance.RandomNumber(0, 100) >= levelPenaltyService.GetGoldPenalty(owner.Level, mob.Level) * 100)
            {
                return;
            }

            goldAmount = (int)(goldAmount * (1 + GoldBonusPercent(owner) / 100d));
        }

        if (goldAmount <= 0 || itemCatalog.GetItemWithEffect(ItemEffectType.Gold) is not { } gold) return;

        try
        {
            var goldItem = itemGenerationService.Create(gold.VNum, (short)System.Math.Min(goldAmount, short.MaxValue));
            var dropped = mapItemGenerationService.Create(mapInstance, goldItem, victim.PositionX, victim.PositionY,
                NextOwner(victim, topDamageDealer));
            await mapInstance.SendPacketAsync(dropped.GenerateDrop());
        }
        catch (System.Exception ex)
        {
            logger.LogWarning(ex, "Failed to spawn gold drop from {Mob}", mob.NpcMonsterVNum);
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
            if (world.IsAlive(handle))
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
