//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    /// <summary>
    /// Reads the family a character belongs to.
    /// </summary>
    public interface IFamilyService
    {
        /// <summary>
        /// The character's membership, with its family and that family's members attached, or
        /// null when the character has no family.
        /// </summary>
        Task<FamilyCharacter?> GetMembershipAsync(long characterId);
    }
}
