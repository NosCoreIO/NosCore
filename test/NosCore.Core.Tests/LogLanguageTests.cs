//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Resource;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.Core.Tests
{
    [TestClass]
    public class LogLanguageTests
    {
        private readonly LogLanguageLocalizer<LogLanguageKey, LocalizedResources> _logLanguageLocalizer;

        public LogLanguageTests()
        {
            var factory = new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()), new LoggerFactory());
            _logLanguageLocalizer = new LogLanguageLocalizer<LogLanguageKey, LocalizedResources>(
                new StringLocalizer<LocalizedResources>(factory));
        }

        [TestCategory("OPTIONAL-TEST")]
        [DataTestMethod]
        [DataRow(RegionType.EN)]
        [DataRow(RegionType.CS)]
        [DataRow(RegionType.DE)]
        [DataRow(RegionType.ES)]
        [DataRow(RegionType.FR)]
        [DataRow(RegionType.IT)]
        [DataRow(RegionType.PL)]
        [DataRow(RegionType.TR)]
        [DataRow(RegionType.RU)]
        public void CheckEveryLanguageValueSet(RegionType type)
        {
            CultureInfo.CurrentCulture = new CultureInfo(type.ToString());

            var result = string.Join(Environment.NewLine, I18NTestHelpers.GetKeysWithMissingTranslations(_logLanguageLocalizer)
                .Select(x => $"value {x} not defined"));

            if (result.Length != 0)
            {
                Assert.Fail(result);
            }
        }

        [DataTestMethod]
        [DataRow(RegionType.EN)]
        [DataRow(RegionType.CS)]
        [DataRow(RegionType.DE)]
        [DataRow(RegionType.ES)]
        [DataRow(RegionType.FR)]
        [DataRow(RegionType.IT)]
        [DataRow(RegionType.PL)]
        [DataRow(RegionType.TR)]
        [DataRow(RegionType.RU)]
        public void CheckEveryLanguageAreUsefull(RegionType type)
        {
            CultureInfo.CurrentUICulture = new CultureInfo(type.ToString());

            var result = string.Join(Environment.NewLine, 
                I18NTestHelpers.GetUselessTranslations(_logLanguageLocalizer, Enum.GetValues(typeof(LanguageKey)).OfType<LanguageKey>().Select(s => s.ToString())
                .Concat(Enum.GetValues(typeof(LogLanguageKey)).OfType<LogLanguageKey>().Select(s => s.ToString())).ToList())
                .Select(x => $"key {x} is useless"));

            if (result.Length != 0)
            {
                Assert.Fail(result);
            }
        }


        [TestMethod]
        public void CheckLanguageUsage()
        {
            var result = string.Join(Environment.NewLine,
                I18NTestHelpers.GetUselessLanguageKeys<LanguageKey>().Cast<Enum>()
                    .Union(I18NTestHelpers.GetUselessLanguageKeys<LogLanguageKey>().Cast<Enum>())
                    .Select(x => $"{x.GetType().Name} {x} is not used!"));
      
            if (result.Length != 0)
            {
                Assert.Fail(result);
            }
        }
    }
}