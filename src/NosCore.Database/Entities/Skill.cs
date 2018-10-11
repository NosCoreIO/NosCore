using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    public class Skill
    {
        #region Instantiation

        #endregion

        #region Properties

        public short AttackAnimation { get; set; }

        public short CastAnimation { get; set; }

        public short CastEffect { get; set; }

        public short CastId { get; set; }

        public short CastTime { get; set; }

        public virtual ICollection<CharacterSkill> CharacterSkill { get; set; } = new HashSet<CharacterSkill>();

        public byte Class { get; set; }

        public virtual ICollection<Combo> Combo { get; set; } = new HashSet<Combo>();

        public short Cooldown { get; set; }

        public byte CPCost { get; set; }

        public short Duration { get; set; }

        public short Effect { get; set; }

        public byte Element { get; set; }

        public byte HitType { get; set; }

        public short ItemVNum { get; set; }

        public byte Level { get; set; }

        public byte LevelMinimum { get; set; }

        public byte MinimumAdventurerLevel { get; set; }

        public byte MinimumArcherLevel { get; set; }

        public byte MinimumMagicianLevel { get; set; }

        public byte MinimumSwordmanLevel { get; set; }

        public short MpCost { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        public virtual ICollection<NpcMonsterSkill> NpcMonsterSkill { get; set; } = new HashSet<NpcMonsterSkill>();

        public int Price { get; set; }

        public byte Range { get; set; }

        public virtual ICollection<ShopSkill> ShopSkill { get; set; } = new HashSet<ShopSkill>();

        public byte SkillType { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short SkillVNum { get; set; }

        public byte TargetRange { get; set; }

        public byte TargetType { get; set; }

        public byte Type { get; set; }

        public short UpgradeSkill { get; set; }

        public short UpgradeType { get; set; }

        public virtual ICollection<BCard> BCards { get; set; } = new HashSet<BCard>();

        #endregion
    }
}