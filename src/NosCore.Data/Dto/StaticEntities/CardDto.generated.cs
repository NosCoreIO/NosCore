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
using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;

namespace NosCore.Data.StaticEntities
{
	/// <summary>
	/// Represents a DTO class for NosCore.Database.Entities.Card.
	/// NOTE: This class is generated by GenerateDtos.tt
	/// </summary>
	public class CardDto : IStaticDto
	{
		[Key]
		public short CardId { get; set; }

	 	public int Duration { get; set; }

	 	public int EffectId { get; set; }

	 	public byte Level { get; set; }

	 	[I18NFrom(typeof(I18NBCardDto))]
		public I18NString Name { get; set; } = new I18NString();
		public string NameI18NKey { get; set; }

	 	public int Delay { get; set; }

	 	public short TimeoutBuff { get; set; }

	 	public byte TimeoutBuffChance { get; set; }

	 	public NosCore.Data.Enumerations.Buff.BCardType.CardType BuffType { get; set; }

	 	public byte Propability { get; set; }

	 }
}