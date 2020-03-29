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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;

namespace NosCore.Tests
{
    [TestClass]
    public class LogLanguageTests
    {
        private readonly Dictionary<string, int> _dict = new Dictionary<string, int>();

        public LogLanguageTests()
        {
            var uselessKeys = new StringBuilder();

            var list = Directory.GetFiles(Environment.CurrentDirectory + @"../../..", "*.cs",
                SearchOption.AllDirectories);
            foreach (var file in list)
            {
                var content = File.ReadAllText(file);
                var regex = new Regex(
                    @"string\.Format\([\s\n]*[Log]*Language.Instance.GetMessageFromKey\((?<key>[\s\n]*LanguageKey\.[\s\n]*[0-9A-Za-z_]*)[\s\n]*,[\s\n]*[\.0-9A-Za-z_]*\)(?<parameter>[\s\n]*,[\s\n]*[\.!?0-9A-Za-z_\[\]]*)*[\s\n]*\)",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
                var matches = regex.Matches(content);
                foreach (Match? match in matches)
                {
                    var param = match?.Groups?.Values?.Where(s => s.Name == "parameter").FirstOrDefault()?.Captures.Count() ?? 0;
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
            var unfound = new StringBuilder();
            foreach (var val in (LanguageKey[])Enum.GetValues(typeof(LanguageKey)))
            {
                var value = Language.Instance.GetMessageFromKey(val, type);
                if (value == $"#<{val.ToString()}>")
                {
                    unfound.Append("\nvalue ").Append(value).Append(" not defined");
                }
            }

            foreach (var val in Enum.GetValues(typeof(LogLanguageKey)))
            {
                var value = LogLanguage.Instance.GetMessageFromKey((LogLanguageKey)val!, type.ToString());
                if (value == $"#<{val}>")
                {
                    unfound.Append("\nvalue ").Append(value).Append(" not defined");
                }
            }


            if (unfound.Length != 0)
            {
                Assert.Fail(unfound.ToString());
            }
        }

        [TestMethod]
        public void CheckLanguageUsage()
        {
            var uselessKeys = new StringBuilder();
            var dict = new Dictionary<string, int>();
            var list = Directory.GetFiles(Environment.CurrentDirectory + @"../../..", "*.cs",
                SearchOption.AllDirectories);
            foreach (var file in list)
            {
                var content = File.ReadAllText(file);
                var regex = new Regex(@"(Log)?LanguageKey\.[0-9A-Za-z_]*");
                var matches = regex.Matches(content);
                foreach (Match? match in matches)
                {
                    if (match?.Success == true)
                    {
                        if (dict.ContainsKey(match.Value))
                        {
                            dict[match.Value]++;
                        }
                        else
                        {
                            dict.Add(match.Value, 1);
                        }
                    }
                }
            }

            foreach (var val in (LanguageKey[])Enum.GetValues(typeof(LanguageKey)))
            {
                var type = val.GetType();
                var typeInfo = type.GetTypeInfo();
                var memberInfo = typeInfo.GetMember(val.ToString());
                var attributes = memberInfo[0].GetCustomAttributes<UsedImplicitlyAttribute>();
                var attribute = attributes.FirstOrDefault();

                if (!dict.ContainsKey($"LanguageKey.{val}") && (attribute == null))
                {
                    uselessKeys.Append("\nLanguageKey ").Append(val).Append(" is not used!");
                }
            }

            foreach (LogLanguageKey val in (LogLanguageKey[]) Enum.GetValues(typeof(LogLanguageKey)))
            {
                var type = val!.GetType();
                var typeInfo = type.GetTypeInfo();
                try
                {
                    var memberInfo = typeInfo.GetMember(val.ToString());
                    var attributes = memberInfo[0].GetCustomAttributes<UsedImplicitlyAttribute>();
                    var attribute = attributes.FirstOrDefault();
               

                if ((dict.ContainsKey($"LogLanguageKey.{val}") == false) && (attribute == null))
                {
                    uselessKeys.Append("\nLogLanguageKey ").Append(val).Append(" is not used!");
                }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            if (uselessKeys.Length != 0)
            {
                Assert.Fail(uselessKeys.ToString());
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
                var value = Language.Instance.GetMessageFromKey((LanguageKey)val!, type);
                var paramCount = Regex.Matches(value, @"{[0-9A-Za-z]}").Count();
                var expectedCount = !_dict.ContainsKey($"LanguageKey.{val}") ? 0
                    : _dict[$"LanguageKey.{val}"];
                if ((value != $"#<{val.ToString()}>") && (expectedCount != paramCount))
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
            var unfound = new StringBuilder();
            var values = Enum.GetValues(typeof(LanguageKey)).OfType<LanguageKey>().Select(s => s.ToString()).ToList();
            var logvalues = Enum.GetValues(typeof(LogLanguageKey)).OfType<LogLanguageKey>().Select(s => s.ToString())
                .ToList();
            foreach (DictionaryEntry? entry in LogLanguage.Instance.GetRessourceSet(type.ToString())!)
            {
                var resourceKey = entry?.Key.ToString() ?? "";
                if (!values.Contains(resourceKey) && !logvalues.Contains(resourceKey))
                {
                    unfound.Append("key ").Append(resourceKey).Append(" is useless\n");
                }
            }

            if (!string.IsNullOrEmpty(unfound.ToString()))
            {
                Assert.Fail(unfound.ToString());
            }
        }
    }
}