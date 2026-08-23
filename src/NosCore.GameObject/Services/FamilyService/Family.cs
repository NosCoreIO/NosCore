//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.FamilyService
{
    public class Family : FamilyDto
    {
        public IReadOnlyList<FamilyCharacter> Members { get; set; } = [];

        /// <summary>
        /// The head's character name, which the family window prints. Kept here because the
        /// head is usually offline, so there is nobody to ask when the window opens.
        /// </summary>
        public string HeadCharacterName { get; set; } = string.Empty;
    }
}
