//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Resource;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NosCore.Core.Tests
{
    [TestClass]
    public class LogLanguageTests
    {
        private readonly LogLanguageLocalizer<LogLanguageKey, LocalizedResources> LogLanguageLocalizer;

        public LogLanguageTests()
        {
            var factory = new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()), new LoggerFactory());
            LogLanguageLocalizer = new LogLanguageLocalizer<LogLanguageKey, LocalizedResources>(
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

            var result = string.Join(Environment.NewLine, I18NTestHelpers.GetKeysWithMissingTranslations(LogLanguageLocalizer)
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
                I18NTestHelpers.GetUselessTranslations(LogLanguageLocalizer, Enum.GetValues(typeof(LanguageKey)).OfType<LanguageKey>().Select(s => s.ToString())
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
            new Spec("Check language usage")
                .Then(AllLanguageKeysShouldBeUsed)
                .Execute();
        }

        private void AllLanguageKeysShouldBeUsed()
        {
            var scripted = KeysUsedByInstanceScripts();
            var result = string.Join(Environment.NewLine,
                I18NTestHelpers.GetUselessLanguageKeys<LanguageKey>().Where(key => !scripted.Contains(key.ToString())).Cast<Enum>()
                    .Union(I18NTestHelpers.GetUselessLanguageKeys<LogLanguageKey>().Cast<Enum>())
                    .Select(x => $"{x.GetType().Name} {x} is not used!"));

            if (result.Length != 0)
            {
                Assert.Fail(result);
            }
        }

        // Instance scripts are the .csx files in the repository's instances folder, which the .cs scan does not read.
        private static HashSet<string> KeysUsedByInstanceScripts()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "NosCore.sln")))
            {
                directory = directory.Parent;
            }

            Assert.IsNotNull(directory, "The repository root was not found above the test output.");
            return Directory.GetFiles(Path.Combine(directory.FullName, "instances"), "*.csx")
                .SelectMany(file => Regex.Matches(File.ReadAllText(file), @"LanguageKey\.([0-9A-Za-z_]+)").Select(match => match.Groups[1].Value))
                .ToHashSet();
        }
    }
}
