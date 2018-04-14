using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Logger;
using NosCore.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NosCore.Test
{
    [TestClass]
    public class LogLanguageTests
    {
        [DataTestMethod]
        [DataRow(RegionType.EN)]
        [DataRow(RegionType.CZ)]
        [DataRow(RegionType.DE)]
        [DataRow(RegionType.ES)]
        [DataRow(RegionType.FR)]
        [DataRow(RegionType.IT)]
        [DataRow(RegionType.PL)]
        [DataRow(RegionType.TR)]
        public void CheckEveryLanguageValueSet(RegionType type)
        {
            string unfound = string.Empty;
            foreach (LanguageKey val in Enum.GetValues(typeof(LanguageKey)))
            {
                string value = LogLanguage.Instance.GetMessageFromKey(val, type.ToString());
                if (value == $"#<{val.ToString()}>")
                {
                    unfound += $"value {value} not defined\n";
                }
            }
            if (!string.IsNullOrEmpty(unfound))
            {
                Assert.Fail(unfound);
            }
        }

        [DataTestMethod]
        [DataRow(RegionType.EN)]
        [DataRow(RegionType.CZ)]
        [DataRow(RegionType.DE)]
        [DataRow(RegionType.ES)]
        [DataRow(RegionType.FR)]
        [DataRow(RegionType.IT)]
        [DataRow(RegionType.PL)]
        [DataRow(RegionType.TR)]
        public void CheckEveryLanguageAreUsefull(RegionType type)
        {
            string unfound = string.Empty;
            var values = Enum.GetValues(typeof(LanguageKey)).OfType<LanguageKey>().Select(s=>s.ToString());
            foreach (DictionaryEntry entry in LogLanguage.Instance.GetRessourceSet(type.ToString()))
            {
                string resourceKey = entry.Key.ToString();
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
