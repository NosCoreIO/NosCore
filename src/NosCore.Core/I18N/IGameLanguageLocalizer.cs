//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Localization;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.Core.I18N;

public interface IGameLanguageLocalizer : ILogLanguageLocalizer
{
    LocalizedString this[LanguageKey key, RegionType region, params object[] arguments] { get; }
    LocalizedString this[LanguageKey key, RegionType region] { get; }
}
