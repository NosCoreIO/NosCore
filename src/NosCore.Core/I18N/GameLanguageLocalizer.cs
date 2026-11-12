//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Localization;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NosCore.Core.I18N
{
    public class GameLanguageLocalizer(ILogLanguageLocalizer<LanguageKey> stringLocalizer) : IGameLanguageLocalizer
    {
        public LocalizedString this[LanguageKey key, RegionType region] =>
            InRegion(region, () => stringLocalizer[key]);

        public LocalizedString this[LanguageKey key, RegionType region, params object[] arguments] =>
            InRegion(region, () => stringLocalizer[key, arguments]);

        // The underlying localizer reads the thread's culture, so it is switched for the lookup only.
        private static LocalizedString InRegion(RegionType region, Func<LocalizedString> lookup)
        {
            var currentUi = CultureInfo.CurrentUICulture;
            var current = CultureInfo.CurrentCulture;
            CultureInfo.CurrentUICulture = new CultureInfo(region.ToString());
            CultureInfo.CurrentCulture = new CultureInfo(region.ToString());

            var result = lookup();

            CultureInfo.CurrentUICulture = currentUi;
            CultureInfo.CurrentCulture = current;
            return result;
        }

        public IEnumerable<LocalizedString> GetAllStrings() => stringLocalizer.GetAllStrings();
    }
}
