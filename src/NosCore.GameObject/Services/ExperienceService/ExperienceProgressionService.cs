//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.FairyExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.SpExperienceService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SkillService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ExperienceService
{
    // Cascades level / job / hero / SP / fairy XP when a player gains experience, emits the
    // level-up packet sequence from the official trace (quest.txt ~line 2075):
    //   levelup <cid> / stat / msgi 510 / eff 6 / eff 198 / (msgi 514 / eff 8) / lev
    // msgi is the i18n variant the current client expects — OpenNos still uses `msg` but
    // that's from before the i18n redesign, so we follow the trace.
    // Overflow XP carries across level boundaries so big grants (quest rewards, GM edicts)
    // telescope cleanly through multiple levels in one call.
    public sealed class ExperienceProgressionService(
        IExperienceService experienceService,
        IJobExperienceService jobExperienceService,
        IHeroExperienceService heroExperienceService,
        ISpExperienceService spExperienceService,
        IFairyExperienceService fairyExperienceService,
        ISkillService skillService) : IExperienceProgressionService
    {
        private const byte MaxLevel = 99;
        private const byte MaxJobLevel = 80;
        private const byte MaxHeroLevel = 60;
        private const byte MaxSpLevel = 99;
        private const byte HeroicStartLevel = 88;

        public async Task AddExperienceAsync(PlayerComponentBundle player,
            long levelXpDelta, long jobXpDelta, long heroXpDelta,
            long spXpDelta, int fairyXpDelta)
        {
            var leveledUp = false;
            var jobLeveledUp = false;
            var heroLeveledUp = false;
            var spLeveledUp = false;

            if (levelXpDelta > 0)
            {
                player.LevelXp += levelXpDelta;
                while (player.Level < MaxLevel)
                {
                    var threshold = experienceService.GetExperience(player.Level);
                    if (threshold <= 0 || player.LevelXp < threshold)
                    {
                        break;
                    }
                    player.LevelXp -= threshold;
                    player.Level = (byte)(player.Level + 1);
                    leveledUp = true;
                    // Crossing level 88 unlocks hero XP (OpenNos Character.cs:5270).
                    if (player.Level == HeroicStartLevel && player.HeroLevel == 0)
                    {
                        player.HeroLevel = 1;
                        player.HeroLevelXp = 0;
                    }
                }
                if (player.Level >= MaxLevel)
                {
                    player.LevelXp = 0;
                }
            }

            if (jobXpDelta > 0)
            {
                // Adventurer caps at job level 20 until class-change (OpenNos Character.cs:5345).
                var jobCap = player.Class == CharacterClassType.Adventurer ? (byte)20 : MaxJobLevel;
                player.JobLevelXp += jobXpDelta;
                while (player.JobLevel < jobCap)
                {
                    var threshold = jobExperienceService.GetJobExperience(player.Class, player.JobLevel);
                    if (threshold <= 0 || player.JobLevelXp < threshold)
                    {
                        break;
                    }
                    player.JobLevelXp -= threshold;
                    player.JobLevel = (byte)(player.JobLevel + 1);
                    jobLeveledUp = true;
                }
                if (player.JobLevel >= jobCap)
                {
                    player.JobLevelXp = 0;
                }
            }

            if (heroXpDelta > 0 && player.HeroLevel > 0)
            {
                player.HeroLevelXp += heroXpDelta;
                while (player.HeroLevel < MaxHeroLevel)
                {
                    var threshold = heroExperienceService.GetHeroExperience(player.HeroLevel);
                    if (threshold <= 0 || player.HeroLevelXp < threshold)
                    {
                        break;
                    }
                    player.HeroLevelXp -= threshold;
                    player.HeroLevel = (byte)(player.HeroLevel + 1);
                    heroLeveledUp = true;
                }
                if (player.HeroLevel >= MaxHeroLevel)
                {
                    player.HeroLevelXp = 0;
                }
            }

            SpecialistInstance? spInstance = null;
            if (spXpDelta > 0 && player.UseSp)
            {
                spInstance = player.InventoryService
                    .LoadBySlotAndType((short)EquipmentType.Sp, NoscorePocketType.Wear)
                    ?.ItemInstance as SpecialistInstance;
                if (spInstance != null && spInstance.SpLevel < MaxSpLevel)
                {
                    spInstance.Xp = (spInstance.Xp ?? 0) + spXpDelta;
                    while (spInstance.SpLevel < MaxSpLevel)
                    {
                        var threshold = spExperienceService.GetSpExperience(spInstance.SpLevel, isSecondarySp: false);
                        if (threshold <= 0 || (spInstance.Xp ?? 0) < threshold)
                        {
                            break;
                        }
                        spInstance.Xp -= threshold;
                        spInstance.SpLevel = (byte)(spInstance.SpLevel + 1);
                        spLeveledUp = true;
                    }
                    if (spInstance.SpLevel >= MaxSpLevel)
                    {
                        spInstance.Xp = 0;
                    }
                }
            }

            WearableInstance? fairy = null;
            var fairyLeveledUp = false;
            var fairyMaxed = false;
            if (fairyXpDelta > 0)
            {
                fairy = player.InventoryService
                    .LoadBySlotAndType((short)EquipmentType.Fairy, NoscorePocketType.Wear)
                    ?.ItemInstance as WearableInstance;
                var baseRate = fairy?.Item?.ElementRate ?? 0;
                var maxRate = fairy?.Item?.MaxElementRate ?? 0;
                if (fairy != null && (fairy.ElementRate ?? 0) + baseRate < maxRate)
                {
                    fairy.Xp = (fairy.Xp ?? 0) + fairyXpDelta;
                    while ((fairy.ElementRate ?? 0) + baseRate < maxRate)
                    {
                        var threshold = fairyExperienceService.GetFairyExperience(
                            (byte)((fairy.ElementRate ?? 0) + baseRate));
                        if (threshold <= 0 || (fairy.Xp ?? 0) < threshold)
                        {
                            break;
                        }
                        fairy.Xp -= threshold;
                        fairy.ElementRate = (short)((fairy.ElementRate ?? 0) + 1);
                        fairyLeveledUp = true;
                    }
                    if ((fairy.ElementRate ?? 0) + baseRate >= maxRate)
                    {
                        fairy.Xp = 0;
                        fairyMaxed = true;
                    }
                }
            }

            var characterLeveledUp = leveledUp || jobLeveledUp || heroLeveledUp || spLeveledUp;

            if (!characterLeveledUp && !fairyLeveledUp)
            {
                await player.SendPacketAsync(player.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
                return;
            }

            var mapInstance = player.MapInstance;

            if (characterLeveledUp)
            {
                // Full heal on any character / SP / job / hero level-up (trace `stat 256 256 78 78`).
                player.Hp = player.MaxHp;
                player.Mp = player.MaxMp;

                await player.SendPacketAsync(new LevelUpPacket { CharacterId = player.CharacterId });
                await player.SendPacketAsync(player.GenerateStat());
                await player.SendPacketAsync(player.GenerateStatInfo());

                if (leveledUp)
                {
                    await player.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.LevelIncreased
                    });
                    await mapInstance.SendPacketAsync(player.GenerateEff(6));
                    await mapInstance.SendPacketAsync(player.GenerateEff(198));
                }

                if (jobLeveledUp)
                {
                    await player.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.JobLevelIncreased
                    });
                    await mapInstance.SendPacketAsync(player.GenerateEff(8));
                    await skillService.LearnClassSkillsAsync(player).ConfigureAwait(false);
                }

                if (heroLeveledUp)
                {
                    await player.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.HeroLevelIncreased
                    });
                }

                if (spLeveledUp)
                {
                    await player.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.SpecialistLevelIncreased
                    });
                    await mapInstance.SendPacketAsync(player.GenerateEff(8));
                    await mapInstance.SendPacketAsync(player.GenerateEff(198));
                }
            }

            if (fairyLeveledUp)
            {
                // Fairy level-up is cosmetic — no stat/levelup/full-heal; refresh `pairy` and
                // push a per-fairy toast. %s is the item name (using vnum for now, client
                // resolves).
                var fairyName = fairy?.Item?.VNum.ToString() ?? string.Empty;
                await player.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = fairyMaxed ? Game18NConstString.ReachedMaxLeve : Game18NConstString.HasLevelledUp,
                    ArgumentType = 10,
                    Game18NArguments = { fairyName }
                });
                await player.SendPacketAsync(player.GeneratePairy(fairy));
            }

            await player.SendPacketAsync(player.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
        }
    }
}
