using System;
using System.Collections;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.Tests
{
    [TestClass]
    public class LogLanguageTests
    {
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
        public void CheckEveryLanguageValueSet(RegionType type)
        {
            var unfound = new StringBuilder();
            foreach (LanguageKey val in Enum.GetValues(typeof(LanguageKey)))
            {
                var value = LogLanguage.Instance.GetMessageFromKey(val, type.ToString());
                if (value == $"#<{val.ToString()}>")
                {
                    unfound.Append($"\nvalue {value} not defined");
                }
            }

            if (unfound.Length != 0)
            {
                Assert.Fail(unfound.ToString());
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
        public void CheckEveryLanguageAreUsefull(RegionType type)
        {
            var unfound = string.Empty;
            var values = Enum.GetValues(typeof(LanguageKey)).OfType<LanguageKey>().Select(s => s.ToString()).ToList();
            foreach (DictionaryEntry entry in LogLanguage.Instance.GetRessourceSet(type.ToString()))
            {
                var resourceKey = entry.Key.ToString();
                if (!values.Contains(resourceKey))
                {
                    unfound += $"key {resourceKey} is useless\n";
                }
            }

            if (!string.IsNullOrEmpty(unfound))
            {
                Assert.Fail(unfound);
            }
        }
    }
}