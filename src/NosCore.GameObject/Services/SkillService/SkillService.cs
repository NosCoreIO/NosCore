using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SkillService
{
    public class SkillService(IDao<CharacterSkillDto, Guid> characterSkillDao, List<SkillDto> skills) : ISkillService
    {
        public async Task LoadSkill(ICharacterEntity character)
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

            await SendSkillListAsync(character, useSpecialist: false);
        }

        private const int FirstSpecialistClass = 31;

        public async Task LoadSpecialistSkillsAsync(ICharacterEntity character, short morph, byte spLevel)
        {
            RemoveSpecialistSkills(character);

            foreach (var skill in skills.Where(s => s.Class > FirstSpecialistClass
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
        }

        public async Task UnloadSpecialistSkillsAsync(ICharacterEntity character)
        {
            RemoveSpecialistSkills(character);
            await SendSkillListAsync(character, useSpecialist: false);
        }

        private static void RemoveSpecialistSkills(ICharacterEntity character)
        {
            foreach (var entry in character.Skills
                         .Where(s => s.Value.Skill?.Class >= FirstSpecialistClass)
                         .ToList())
            {
                character.Skills.TryRemove(entry.Key, out _);
            }
        }

        private async Task SendSkillListAsync(ICharacterEntity character, bool useSpecialist)
        {
            var ordered = character.Skills.Values
                .Where(s => s.Skill != null && (s.Skill!.Class >= FirstSpecialistClass) == useSpecialist)
                .OrderBy(s => s.Skill!.CastId)
                .Select(s => s.SkillVNum)
                .ToList();

            var classByte = (byte)character.Class;
            var primary = useSpecialist
                ? (ordered.Count > 0 ? ordered[0] : (short)0)
                : (short)(200 + 20 * classByte);
            var secondary = useSpecialist
                ? (ordered.Count > 0 ? ordered[0] : (short)0)
                : (short)(201 + 20 * classByte);

            await character.SendPacketAsync(new SkiPacket
            {
                PrimarySkillVnum = primary,
                SecondarySkillVnum = secondary,
                SkillVnums = ordered,
            }).ConfigureAwait(false);
        }

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
        private IEnumerable<SkillDto> Learnable(ICharacterEntity character) =>
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
        public async Task ForgetUnlearnableSkillsAsync(ICharacterEntity character)
        {
            var keep = Learnable(character)
                .Where(skill => skill.LevelMinimum <= character.JobLevel)
                .Select(skill => skill.SkillVNum)
                .ToHashSet();
            var characterId = character.VisualId;

            foreach (var stale in characterSkillDao.Where(x => x.CharacterId == characterId)?
                         .Where(x => !keep.Contains(x.SkillVNum)).ToList() ?? [])
            {
                await characterSkillDao.TryDeleteAsync(stale.Id).ConfigureAwait(false);
                character.Skills.TryRemove(stale.SkillVNum, out _);
            }
        }

        public async Task<bool> LearnClassSkillsAsync(ICharacterEntity character)
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
    }
}
