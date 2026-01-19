//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Localization;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Globalization;

namespace NosCore.Core.I18N
{
    public class GameLanguageLocalizer(ILogLanguageLocalizer<LanguageKey> stringLocalizer) : IGameLanguageLocalizer
    {
        public LocalizedString this[LanguageKey key, RegionType region]
        {
            get
            {
                var currentUi = CultureInfo.CurrentUICulture;
                var current = CultureInfo.CurrentCulture;
                CultureInfo.CurrentUICulture = new CultureInfo(region.ToString());
                CultureInfo.CurrentCulture = new CultureInfo(region.ToString());

                var result = stringLocalizer[key];

                CultureInfo.CurrentUICulture = currentUi;
                CultureInfo.CurrentCulture = current;
                return result;
            }
        }

        public LocalizedString this[LanguageKey key, RegionType region, params object[] arguments]
        {
            get
            {
                var currentUi = CultureInfo.CurrentUICulture;
                var current = CultureInfo.CurrentCulture;
                CultureInfo.CurrentUICulture = new CultureInfo(region.ToString());
                CultureInfo.CurrentCulture = new CultureInfo(region.ToString());

                var result = stringLocalizer[key, arguments];

                CultureInfo.CurrentUICulture = currentUi;
                CultureInfo.CurrentCulture = current;
                return result;
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings() => stringLocalizer.GetAllStrings();
    }
}
