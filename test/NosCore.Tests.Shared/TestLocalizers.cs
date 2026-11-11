//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Localization;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.Tests.Shared
{
    /// <summary>Localizers that echo the key, so a test can assert on which message was chosen.</summary>
    public static class TestLocalizers
    {
        public static ILogLanguageLocalizer<LogLanguageKey> Log { get; } = CreateLog();

        public static IGameLanguageLocalizer Game { get; } = CreateGame();

        private static ILogLanguageLocalizer<LogLanguageKey> CreateLog()
        {
            var localizer = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            localizer.Setup(x => x[It.IsAny<LogLanguageKey>()])
                .Returns((LogLanguageKey key) => new LocalizedString(key.ToString(), key.ToString(), false));
            return localizer.Object;
        }

        private static IGameLanguageLocalizer CreateGame()
        {
            var localizer = new Mock<IGameLanguageLocalizer>();
            localizer.Setup(x => x[It.IsAny<LanguageKey>(), It.IsAny<RegionType>()])
                .Returns((LanguageKey key, RegionType region) => new LocalizedString($"{key}{region}", $"{key}{region}", false));
            return localizer.Object;
        }
    }
}
