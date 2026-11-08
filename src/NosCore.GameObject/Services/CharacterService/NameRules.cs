//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Text.RegularExpressions;

namespace NosCore.GameObject.Services.CharacterService
{
    // Character and family names share one character set; it has no space, so a name always fits a
    // single packet field.
    public static partial class NameRules
    {
        [GeneratedRegex(@"^[!-~¡-¬®-ÿ一-龥ก-ฺ฿-๛.]*$")]
        private static partial Regex AllowedCharacters();

        public static bool HasOnlyAllowedCharacters(string name) => AllowedCharacters().IsMatch(name);
    }
}
