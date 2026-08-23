//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.FamilyService
{
    public class Family : FamilyDto
    {
        public IReadOnlyList<FamilyCharacterDto> Members { get; set; } = [];

        /// <summary>
        /// The head's character name, which the family window prints. Kept here because the
        /// head is usually offline, so there is nobody to ask when the window opens.
        /// </summary>
        public string HeadCharacterName { get; set; } = string.Empty;

        /// <summary>
        /// A rank that is not in the member list is Member: the packet has no way to say "none",
        /// and the lowest rank is the one that grants nothing.
        /// </summary>
        public FamilyAuthority AuthorityOf(long characterId) =>
            Members.FirstOrDefault(s => s.CharacterId == characterId)?.Authority
            ?? FamilyAuthority.Member;
    }
}
