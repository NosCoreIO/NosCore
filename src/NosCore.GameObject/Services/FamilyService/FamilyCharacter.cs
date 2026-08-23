//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;

namespace NosCore.GameObject.Services.FamilyService
{
    /// <summary>
    /// One character's membership of one family.
    /// </summary>
    public class FamilyCharacter : FamilyCharacterDto
    {
        /// <summary>
        /// The member's character name.
        /// </summary>
        /// <remarks>
        /// Carried here rather than looked up each time: the family window names the head, and
        /// the head is usually offline. Reading it once when the family is loaded costs one query
        /// instead of one per window opening.
        /// </remarks>
        public string CharacterName { get; set; } = string.Empty;

        /// <summary>The family this membership belongs to.</summary>
        public Family Family { get; set; } = null!;
    }
}
