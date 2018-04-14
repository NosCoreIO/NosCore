using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Logger;
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
        [TestMethod]
        public void CheckEveryLanguageValueSet()
        {
            string unfound = string.Empty;
            foreach (LanguageKey val in Enum.GetValues(typeof(LanguageKey)))
            {
                string value = LogLanguage.Instance.GetMessageFromKey(val);
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

        [TestMethod]
        public void CheckEveryLanguageAreUsefull()
        {
            string unfound = string.Empty;
            var values = Enum.GetValues(typeof(LanguageKey)).OfType<LanguageKey>();
            foreach (DictionaryEntry entry in LogLanguage.Instance.GetRessourceSet())
            {
                string resourceKey = entry.Key.ToString();
                if (!values.Any())
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
