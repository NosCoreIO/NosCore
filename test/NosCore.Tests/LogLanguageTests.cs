//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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