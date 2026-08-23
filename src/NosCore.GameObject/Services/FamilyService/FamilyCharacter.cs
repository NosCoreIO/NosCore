//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyCharacter : FamilyCharacterDto
    {
        public Family Family { get; set; } = null!;
    }
}
