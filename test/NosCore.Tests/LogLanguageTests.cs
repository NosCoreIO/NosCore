using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Logger;
using System;
using System.Collections.Generic;
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
    }
}
