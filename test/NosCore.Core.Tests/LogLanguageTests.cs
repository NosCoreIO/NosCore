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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Resource;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.Core.Tests
{
    [TestClass]
    public class LogLanguageTests
    {
        private readonly Dictionary<string, int> _dict = new();
        private readonly LogLanguageLocalizer<LogLanguageKey, LocalizedResources> _logLanguageLocalizer;

        public LogLanguageTests()
        {
            var factory = new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()), new LoggerFactory());
            _logLanguageLocalizer = new LogLanguageLocalizer<LogLanguageKey, LocalizedResources>(
                new StringLocalizer<LocalizedResources>(factory));

            var uselessKeys = new StringBuilder();

            var list = Directory.GetFiles(Environment.CurrentDirectory + @"../../..", "*.cs",
                SearchOption.AllDirectories);
            foreach (var file in list)
            {
                var content = File.ReadAllText(file);
                var regex = new Regex(
                    @"string\.Format\([\s\n]*[(Log)|(Game)]*Language.Instance.GetMessageFromKey\((?<key>[\s\n]*LanguageKey\.[\s\n]*[0-9A-Za-z_]*)[\s\n]*,[\s\n]*[\.0-9A-Za-z_]*\)(?<parameter>[\s\n]*,[\s\n]*[\.!?0-9A-Za-z_\[\]]*)*[\s\n]*\)",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
                var matches = regex.Matches(content);
                foreach (Match? match in matches)
                {
                    var param = match?.Groups?.Values?.Where(s => s.Name == "parameter").FirstOrDefault()?.Captures.Count ?? 0;
                    var key = match?.Groups?.Values?.FirstOrDefault(s => s.Name == "key")?.Value ?? "";
                    if (_dict.ContainsKey(key))
                    {
                        if (_dict[key] != param)
                        {
                            Assert.Fail(uselessKeys.ToString());
                        }
                    }
                    else
                    {
                        _dict.Add(key, param);
                    }
                }
            }
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
                I18NTestHelpers.GetUselessLanguageKeys<LanguageKey>()
                    .Select(x => $"{x.GetType().Name} {x} is  not used!"));
            result += Environment.NewLine + string.Join(Environment.NewLine,
                I18NTestHelpers.GetUselessLanguageKeys<LogLanguageKey>()
                    .Select(x => $"{x.GetType().Name} {x} is  not used!"));

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
        public void CheckParametersLanguages(RegionType type)
        {
            var unfound = new StringBuilder();
            foreach (var val in Enum.GetValues(typeof(LanguageKey)))
            {
                var value = GameLanguage.Instance.GetMessageFromKey((LanguageKey)val!, type);
                var paramCount = Regex.Matches(value, @"{[0-9A-Za-z]}").Count;
                var expectedCount = !_dict.ContainsKey($"LanguageKey.{val}") ? 0
                    : _dict[$"LanguageKey.{val}"];
                if ((value != $"#<{val}>") && (expectedCount != paramCount))
                {
                    unfound.Append(val)
                        .Append(
                            $" does not contain the correct amount of parameters. Expected:{expectedCount} Given: {paramCount}");
                }
            }

            if (unfound.Length != 0)
            {
                Assert.Fail(unfound.ToString());
            }
        }

    }
}