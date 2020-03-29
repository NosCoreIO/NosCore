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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NosCore.Packets.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class Character : IEntity
    {
        public Character()
        {
            CharacterSkill = new HashSet<CharacterSkill>();
            CharacterRelation1 = new HashSet<CharacterRelation>();
            CharacterRelation2 = new HashSet<CharacterRelation>();
            StaticBonus = new HashSet<StaticBonus>();
            Title = new HashSet<Title>();
            StaticBuff = new HashSet<StaticBuff>();
            BazaarItem = new HashSet<BazaarItem>();
            Inventory = new HashSet<InventoryItemInstance>();
            CharacterActParts = new HashSet<CharacterActPart>();
            QuicklistEntry = new HashSet<QuicklistEntry>();
            Respawn = new HashSet<Respawn>();
            Mail = new HashSet<Mail>();
            Mail1 = new HashSet<Mail>();
            Mate = new HashSet<Mate>();
            CharacterQuest = new HashSet<CharacterQuest>();
            Miniland = new HashSet<Miniland>();
            Warehouses = new HashSet<Warehouse>();
            FamilyCharacter = new HashSet<FamilyCharacter>();
        }

        public virtual Account Account { get; set; } = null!;

        public long AccountId { get; set; }

        public int Act4Dead { get; set; }

        public int Act4Kill { get; set; }

        public int Act4Points { get; set; }

        public int ArenaWinner { get; set; }

        public virtual ICollection<BazaarItem> BazaarItem { get; set; }

        public virtual ICollection<Miniland> Miniland { get; set; }

        public virtual ICollection<CharacterActPart> CharacterActParts { get; set; }

        [MaxLength(255)]
        public string? Biography { get; set; }

        public bool BuffBlocked { get; set; }

        [Key]
        public long CharacterId { get; set; }

        public virtual ICollection<CharacterRelation> CharacterRelation1 { get; set; }

        public virtual ICollection<CharacterRelation> CharacterRelation2 { get; set; }

        public virtual ICollection<CharacterSkill> CharacterSkill { get; set; }

        public CharacterClassType Class { get; set; }

        public short Compliment { get; set; }

        public float Dignity { get; set; }

        public int Elo { get; set; }

        public bool EmoticonsBlocked { get; set; }

        public bool ExchangeBlocked { get; set; }

        public byte Faction { get; set; }

        public virtual ICollection<FamilyCharacter> FamilyCharacter { get; set; }

        public bool FamilyRequestBlocked { get; set; }

        public bool FriendRequestBlocked { get; set; }

        public GenderType Gender { get; set; }

        public long Gold { get; set; }

        public bool GroupRequestBlocked { get; set; }

        public HairColorType HairColor { get; set; }

        public HairStyleType HairStyle { get; set; }

        public bool HeroChatBlocked { get; set; }

        public byte HeroLevel { get; set; }

        public long HeroXp { get; set; }

        public int Hp { get; set; }

        public bool HpBlocked { get; set; }

        public virtual ICollection<InventoryItemInstance> Inventory { get; set; }

        public byte JobLevel { get; set; }

        public long JobLevelXp { get; set; }

        public byte Level { get; set; }

        public long LevelXp { get; set; }

        public virtual ICollection<Mail> Mail { get; set; }

        public virtual ICollection<Mail> Mail1 { get; set; }

        public virtual Map Map { get; set; } = null!;

        public short MapId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public int MasterPoints { get; set; }

        public int MasterTicket { get; set; }

        public virtual ICollection<CharacterQuest> CharacterQuest { get; set; }

        public virtual ICollection<Mate> Mate { get; set; }

        public byte MaxMateCount { get; set; }

        public bool MinilandInviteBlocked { get; set; }

        public bool MouseAimLock { get; set; }

        public int Mp { get; set; }

        [MaxLength(25)]
        public string? Prefix { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = "";

        public bool QuickGetUp { get; set; }

        public virtual ICollection<QuicklistEntry> QuicklistEntry { get; set; }

        public long RagePoint { get; set; }

        public long Reput { get; set; }

        public virtual ICollection<Respawn> Respawn { get; set; }

        public byte Slot { get; set; }

        public int SpAdditionPoint { get; set; }

        public int SpPoint { get; set; }

        public CharacterState State { get; set; }

        public virtual ICollection<StaticBonus> StaticBonus { get; set; }
        public virtual ICollection<Title> Title { get; set; }
        public virtual ICollection<StaticBuff> StaticBuff { get; set; }

        public int TalentLose { get; set; }

        public int TalentSurrender { get; set; }

        public int TalentWin { get; set; }

        public bool WhisperBlocked { get; set; }
        public virtual ICollection<Warehouse> Warehouses { get; set; }
    }
}