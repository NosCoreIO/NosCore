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
            await ForgetSkillsOfOtherClassesAsync(character).ConfigureAwait(false);

            var characterSkills = characterSkillDao.Where(x => x.CharacterId == character.VisualId).Adapt<List<CharacterSkill>>() ?? new List<CharacterSkill>();
            var skillToUse = skills.Where(x => characterSkills.Select(s => s.SkillVNum).Contains(x.SkillVNum));
            character.Skills.Clear();
            foreach (var characterSkill in characterSkills)
            {
                characterSkill.Skill = skillToUse.First(x => x.SkillVNum == characterSkill.SkillVNum);
                character.Skills.AddOrUpdate(characterSkill.SkillVNum, characterSkill,
                    (key, oldValue) => characterSkill);
            }

            // Push the refreshed list to the client. Matches OpenNos GenerateSki:
            // primary/secondary are the class starter vnums (200 + 20*Class, +1),
            // followed by every learned skill ordered by cast id so the bar draws
            // deterministically. Without this packet the client's hotbar stays empty
            // and the server's skill-cast gate is invisible.
            var classByte = (byte)character.Class;
            var ordered = character.Skills.Values
                .Where(s => s.Skill != null)
                .OrderBy(s => s.Skill!.CastId)
                .Select(s => s.SkillVNum)
                .ToList();

            await character.SendPacketAsync(new SkiPacket
            {
                PrimarySkillVnum = (short)(200 + 20 * classByte),
                SecondarySkillVnum = (short)(201 + 20 * classByte),
                SkillVnums = ordered,
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// The Adventurer's skills, listed one by one.
        ///
        /// <b>They cannot be selected by class like every other one</b>, and that is this table's
        /// trap: class 0 does not mean "Adventurer", it is the scrap container where 193 entries
        /// end up - passives, monster skills, things with no cost and no cast. Filtering by class
        /// 0 gave an Adventurer <i>all</i> of them, and the bar filled with icons the client will
        /// not cast: the symptom is "the skills arrive but they do not work".
        ///
        /// These are the real numbers, from the original game. 209 does not exist.
        /// </summary>
        private static readonly short[] AdventurerSkills =
            { 200, 201, 202, 203, 204, 205, 206, 207, 208, 210 };

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
        /// </summary>
        public async Task ForgetSkillsOfOtherClassesAsync(ICharacterEntity character)
        {
            var keep = Learnable(character).Select(s => s.SkillVNum).ToHashSet();
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

            var ordered = character.Skills.Values
                .Where(s => s.Skill != null)
                .OrderBy(s => s.Skill!.CastId)
                .Select(s => s.SkillVNum)
                .ToList();

            await character.SendPacketAsync(new SkiPacket
            {
                PrimarySkillVnum = (short)(200 + 20 * classByte),
                SecondarySkillVnum = (short)(201 + 20 * classByte),
                SkillVnums = ordered,
            }).ConfigureAwait(false);
            return true;
        }
    }
}
