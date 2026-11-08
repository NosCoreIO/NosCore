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

        /// <summary>The head's name, kept here because the head is usually offline.</summary>
        public string HeadCharacterName { get; set; } = string.Empty;

        /// <summary>Not in the member list means Member: the packet cannot say "none".</summary>
        public FamilyAuthority AuthorityOf(long characterId) =>
            Members.FirstOrDefault(s => s.CharacterId == characterId)?.Authority
            ?? FamilyAuthority.Member;

        public bool CanInvite(long characterId) => AuthorityOf(characterId) switch
        {
            FamilyAuthority.Head or FamilyAuthority.Assistant => true,
            FamilyAuthority.Manager => ManagerCanInvite,
            _ => false
        };

        public bool CanGetHistory(long characterId) => AuthorityOf(characterId) switch
        {
            FamilyAuthority.Head or FamilyAuthority.Assistant => true,
            FamilyAuthority.Manager => ManagerCanGetHistory,
            _ => MemberCanGetHistory
        };
    }
}
