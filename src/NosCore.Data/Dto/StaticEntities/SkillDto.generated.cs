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

using System.ComponentModel.DataAnnotations;
using NosCore.Data.I18N;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using Mapster;

namespace NosCore.Data.StaticEntities
{
	/// <summary>
	/// Represents a DTO class for NosCore.Database.Entities.Skill.
	/// NOTE: This class is generated by GenerateDtos.tt
	/// </summary>
	public class SkillDto : IStaticDto
	{
		public short AttackAnimation { get; set; }

	 	public short CastAnimation { get; set; }

	 	public short CastEffect { get; set; }

	 	public short CastId { get; set; }

	 	public short CastTime { get; set; }

	 	[AdaptIgnore]
	// foreign key injection of CharacterSkill
		public System.Collections.Generic.ICollection<CharacterSkillDto> CharacterSkillDto { get; set; }

	 	public byte Class { get; set; }

	 	[AdaptIgnore]
	// foreign key injection of Combo
		public System.Collections.Generic.ICollection<ComboDto> ComboDto { get; set; }

	 	public short Cooldown { get; set; }

	 	public byte CpCost { get; set; }

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

	 	[I18NFrom(typeof(I18NSkillDto))]
		public I18NString Name { get; set; } = new I18NString();
		[AdaptMember("Name")]
		public string NameI18NKey { get; set; }

	 	[AdaptIgnore]
	// foreign key injection of NpcMonsterSkill
		public System.Collections.Generic.ICollection<NpcMonsterSkillDto> NpcMonsterSkillDto { get; set; }

	 	public int Price { get; set; }

	 	public byte Range { get; set; }

	 	[AdaptIgnore]
	// foreign key injection of ShopSkill
		public System.Collections.Generic.ICollection<ShopSkillDto> ShopSkillDto { get; set; }

	 	public byte SkillType { get; set; }

	 	[Key]
		public short SkillVNum { get; set; }

	 	public byte TargetRange { get; set; }

	 	public byte TargetType { get; set; }

	 	public byte Type { get; set; }

	 	public short UpgradeSkill { get; set; }

	 	public short UpgradeType { get; set; }

	 	[AdaptIgnore]
	// foreign key injection of BCards
		public System.Collections.Generic.ICollection<BCardDto> BCardsDto { get; set; }

	 }
}