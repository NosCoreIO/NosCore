using Mapster;
using NosCore.GameObject.Ecs;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SpExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SkillService
{
    public class SkillService(IDao<CharacterSkillDto, Guid> characterSkillDao, List<SkillDto> skills,
        IVitalityService vitalityService, IBattleStatsProvider battleStatsProvider,
        IExperienceService experienceService, IJobExperienceService jobExperienceService,
        IHeroExperienceService heroExperienceService, ISpExperienceService spExperienceService, ISkillPointService skillPointService) : ISkillService
    {
        public async Task LoadSkill(PlayerComponentBundle character)
        {
            // Characters who changed class before the deletion below existed are still carrying
            // the old rows. Clearing them at login, and not only at the next class change, means
            // those characters heal themselves instead of staying broken for ever.
            await ForgetUnlearnableSkillsAsync(character).ConfigureAwait(false);

            var characterSkills = characterSkillDao.Where(x => x.CharacterId == character.VisualId).Adapt<List<CharacterSkill>>() ?? new List<CharacterSkill>();
            var skillToUse = skills.Where(x => characterSkills.Select(s => s.SkillVNum).Contains(x.SkillVNum));
            character.Skills.Clear();
            foreach (var characterSkill in characterSkills)
            {
                characterSkill.Skill = skillToUse.First(x => x.SkillVNum == characterSkill.SkillVNum);
                character.Skills.AddOrUpdate(characterSkill.SkillVNum, characterSkill,
                    (key, oldValue) => characterSkill);
            }
        }

        public async Task LoadSpecialistSkillsAsync(PlayerComponentBundle character, short morph, byte spLevel)
        {
            RemoveSpecialistSkills(character);

            foreach (var skill in skills.Where(s => s.IsSpecialist()
                                                    && s.UpgradeType == morph
                                                    && s.LevelMinimum <= spLevel))
            {
                character.Skills.AddOrUpdate(skill.SkillVNum,
                    new CharacterSkill
                    {
                        SkillVNum = skill.SkillVNum,
                        CharacterId = character.VisualId,
                        Skill = skill
                    },
                    (_, existing) => existing);
            }

            await SendSkillListAsync(character, useSpecialist: true);
            await character.SendPacketsAsync(character.GenerateQuicklist());
        }

        public async Task UnloadSpecialistSkillsAsync(PlayerComponentBundle character)
        {
            RemoveSpecialistSkills(character);
            await SendSkillListAsync(character, useSpecialist: false);
            await character.SendPacketsAsync(character.GenerateQuicklist());
        }

        private static void RemoveSpecialistSkills(PlayerComponentBundle character)
        {
            foreach (var entry in character.Skills
                         .Where(s => s.Value.Skill?.IsSpecialist() == true)
                         .ToList())
            {
                character.Skills.TryRemove(entry.Key, out _);
            }
        }

        private static Task SendSkillListAsync(PlayerComponentBundle character, bool useSpecialist)
            => character.SendPacketAsync(character.GenerateSki(useSpecialist));

        /// <summary>
        /// The Adventurer's skills, listed one by one.
        ///
        /// <b>They cannot be selected by class like every other one</b>, and that is this table's
        /// trap: class 0 does not mean "Adventurer", it is the scrap container where 193 entries
        /// end up - the emotes, the stat courses, the shop and rest actions, passives. Filtering
        /// by class 0 gave an Adventurer <i>all</i> of them, and the bar filled with icons the
        /// client will not cast.
        ///
        /// The range is 200 to 210 inclusive. 209 is in it: Skill.dat gives it class 0, cast id
        /// 16, LevelMinimum 1 and the name Capture - it is the Adventurer's pet catcher, which is
        /// why CharNewPacketHandler grants it to every new character and why its comment there
        /// talks about <c>u_s 16</c>. Left out, an Adventurer cannot catch anything.
        ///
        /// 211 and 212 are excluded on purpose: the file calls them "Ultra Super Cheating Skill"
        /// and "Admin Cheating Skill".
        ///
        /// Open, and deliberately not guessed at: 300 to 306 are also class 0, with LevelMinimum
        /// 10 to 18 and names like "Strengthen Swing". They look like the upgraded forms of
        /// 200-206 and they carry the SAME cast ids, so they cannot simply be added alongside.
        /// Nothing in the files says how one replaces the other, so they stay out until it does.
        /// </summary>
        private static readonly short[] AdventurerSkills =
            { 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210 };

        /// <summary>The skills this class can hold.</summary>
        private IEnumerable<SkillDto> Learnable(PlayerComponentBundle character) =>
            character.Class == CharacterClassType.Adventurer
                ? skills.Where(s => AdventurerSkills.Contains(s.SkillVNum))
                : skills.Where(s => s.Class == (byte)character.Class);

        /// <summary>
        /// A class change was only half done. The change empties the in-memory list and learns the
        /// new class's skills, but nothing ever deleted the rows behind the old ones - so the next
        /// login loaded both sets back.
        ///
        /// That is not a cosmetic leftover. Cast ids are numbered per class and start at zero, so
        /// an Archer who used to be an Adventurer ended up knowing two skills answering to cast 0:
        /// Swing (melee) and Archery (ranged). Which one the resolver returned came down to
        /// dictionary order.
        ///
        /// The visible symptom was a basic attack computed off the <b>wrong weapon</b>: Swing is a
        /// melee skill, a melee skill selects the secondary-weapon profile on an Archer, and the
        /// bow in the main hand counted for nothing.
        ///
        /// The job level is part of the question and not only the class: a class change puts the
        /// job level back to 1, so a row for a skill of the <i>destination</i> class that needs
        /// job 20 is just as unusable as one belonging to the class left behind.
        /// </summary>
        public async Task ForgetUnlearnableSkillsAsync(PlayerComponentBundle character)
        {
            var keep = Learnable(character)
                .Where(skill => skill.LevelMinimum <= character.JobLevel)
                .Select(skill => skill.SkillVNum)
                .ToHashSet();
            var characterId = character.VisualId;

            var passives = skills.Where(s => s.IsPassive()).Select(s => s.SkillVNum).ToHashSet();
            foreach (var stale in characterSkillDao.Where(x => x.CharacterId == characterId)?
                         .Where(x => !keep.Contains(x.SkillVNum) && !passives.Contains(x.SkillVNum)).ToList() ?? [])
            {
                await characterSkillDao.TryDeleteAsync(stale.Id).ConfigureAwait(false);
                character.Skills.TryRemove(stale.SkillVNum, out _);
            }
        }

        public async Task<bool> LearnClassSkillsAsync(PlayerComponentBundle character)
        {
            var classByte = (byte)character.Class;
            var learned = false;
            foreach (var skill in Learnable(character).Where(s => s.LevelMinimum <= character.JobLevel))
            {
                if (character.Skills.ContainsKey(skill.SkillVNum))
                {
                    continue;
                }

                // The row's existing database id is reused when there is one. With a fresh Guid
                // every time, each call inserted one more row for the same skill: in memory it did
                // not show, because the dictionary is keyed by skill number and collapses them,
                // but the rows piled up behind it.
                var characterId = character.VisualId;
                var skillVNum = skill.SkillVNum;
                var existing = await characterSkillDao
                    .FirstOrDefaultAsync(x => x.CharacterId == characterId && x.SkillVNum == skillVNum)
                    .ConfigureAwait(false);

                var entry = new CharacterSkill
                {
                    Id = existing?.Id ?? Guid.NewGuid(),
                    CharacterId = characterId,
                    SkillVNum = skillVNum,
                    Skill = skill,
                };
                if (character.Skills.TryAdd(skill.SkillVNum, entry))
                {
                    await characterSkillDao.TryInsertOrUpdateAsync(entry.Adapt<CharacterSkillDto>()).ConfigureAwait(false);
                    learned = true;
                }
            }

            if (!learned)
            {
                return false;
            }

            await SendSkillListAsync(character, useSpecialist: false);
            return true;
        }

        public Task BuyAsync(PlayerComponentBundle character, SkillDto skill) => TeachAsync(character, skill, skill.Price);

        public async Task<bool> LearnFromBookAsync(PlayerComponentBundle character, short skillVNum)
        {
            var skill = skills.FirstOrDefault(s => s.SkillVNum == skillVNum);
            if (skill == null || !skill.IsPassive())
            {
                return false;
            }

            return await TeachAsync(character, skill, 0);
        }

        private async Task<bool> TeachAsync(PlayerComponentBundle character, SkillDto skill, int price)
        {
            var refusal = LearningRefusal(character, skill, price);
            if (refusal != null)
            {
                await character.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = refusal.Value });
                return false;
            }

            var entry = new CharacterSkill
            {
                Id = Guid.NewGuid(),
                CharacterId = character.VisualId,
                SkillVNum = skill.SkillVNum,
                Skill = skill,
            };
            if (await characterSkillDao.TryInsertOrUpdateAsync(entry.Adapt<CharacterSkillDto>()) == null)
            {
                return false;
            }

            if (skill.IsPassive())
            {
                foreach (var weaker in character.Skills.Values
                             .Where(s => s.Skill.IsPassive() && s.Skill.CastId == skill.CastId).ToList())
                {
                    await characterSkillDao.TryDeleteAsync(weaker.Id);
                    character.Skills.TryRemove(weaker.SkillVNum, out _);
                }
            }

            character.Skills[skill.SkillVNum] = entry;
            if (price > 0)
            {
                character.Gold -= price;
                await character.AddGoldAsync();
            }

            await SendSkillListAsync(character, useSpecialist: false);
            if (character is Ecs.PlayerComponentBundle player)
            {
                if (skill.IsPassive())
                {
                    await vitalityService.RefreshAndNotifyAsync(player);
                    await player.SendPacketAsync(player.GenerateSc(battleStatsProvider));
                }
                else
                {
                    await player.SendPacketAsync(player.GenerateLev(experienceService, jobExperienceService,
                        heroExperienceService, spExperienceService, skillPointService));
                }
            }

            await character.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.SkillLearned });
            return true;
        }

        private Game18NConstString? LearningRefusal(PlayerComponentBundle character, SkillDto skill, int price)
        {
            if (character.Skills.ContainsKey(skill.SkillVNum))
            {
                return Game18NConstString.YouAlreadyKnowThisSkill;
            }

            if (skill.IsPassive())
            {
                var level = PassiveLevel(skill, character.Class);
                var characterLevel = skill.MinimumLevelIsChampionLevel ? character.HeroLevel : character.Level;
                if (level == 0 || characterLevel < level)
                {
                    return Game18NConstString.YouCanNotLearnThat;
                }

                if (character.Skills.Values.Any(s => s.Skill.IsPassive() && s.Skill.CastId == skill.CastId
                                                     && PassiveLevel(s.Skill, character.Class) > level))
                {
                    return Game18NConstString.YouAlreadyKnowABetterSkill;
                }
            }
            else
            {
                if (!Learnable(character).Any(s => s.SkillVNum == skill.SkillVNum)
                    || character.JobLevel < skill.LevelMinimum)
                {
                    return Game18NConstString.YouCanNotLearnThat;
                }

                if (skill.IsUpgrade() && !character.Skills.ContainsKey(skill.UpgradeSkill))
                {
                    return Game18NConstString.YouNeedBaseSkill;
                }
            }

            if (character.Gold < price)
            {
                return Game18NConstString.NotEnoughGold;
            }

            return !skill.IsPassive() && skill.CpCost > character.RemainingSkillPoints(skillPointService)
                ? Game18NConstString.NotEnoughCp
                : null;
        }

        // A passive open to every class only fills the Adventurer column.
        private static byte PassiveLevel(SkillDto skill, CharacterClassType characterClass)
        {
            if (skill is { MinimumSwordmanLevel: 0, MinimumArcherLevel: 0, MinimumMagicianLevel: 0 })
            {
                return skill.MinimumAdventurerLevel;
            }

            return characterClass switch
            {
                CharacterClassType.Swordsman => skill.MinimumSwordmanLevel,
                CharacterClassType.Archer => skill.MinimumArcherLevel,
                CharacterClassType.Mage => skill.MinimumMagicianLevel,
                _ => skill.MinimumAdventurerLevel,
            };
        }
    }
}
