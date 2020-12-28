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

using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Enumerations;
using System.Globalization;
using System.Resources;

namespace NosCore.Core.I18N
{
    public sealed class LogLanguage
    {
        private static LogLanguage? _instance;

        private static readonly CultureInfo ResourceCulture = new CultureInfo(Language.ToString());

        private readonly ResourceManager _manager;

        private LogLanguage()
        {
            var assem = typeof(LogLanguageKey).Assembly;
            _manager = new ResourceManager(
                assem.GetName().Name + ".Resource.LocalizedResources",
                assem);
        }

        public static RegionType Language { get; set; }

        public static LogLanguage Instance => _instance ??= new LogLanguage();

        public string GetMessageFromKey(LogLanguageKey messageKey)
        {
            return GetMessageFromKey(messageKey, null);
        }

        public string GetMessageFromKey(LogLanguageKey messageKey, string? culture)
        {
            var cult = culture != null ? new CultureInfo(culture) : ResourceCulture;
            var resourceMessage = (_manager != null)
                ? _manager.GetResourceSet(cult, true,
                        cult.TwoLetterISOLanguageName == default(RegionType).ToString().ToLower(cult))
                    ?.GetString(messageKey.ToString()) : string.Empty;

            return !string.IsNullOrEmpty(resourceMessage) ? resourceMessage : $"#<{messageKey}>";
        }

        public ResourceSet? GetRessourceSet(string? culture)
        {
            return _manager.GetResourceSet(culture != null ? new CultureInfo(culture) : ResourceCulture, true, true);
        }
    }
}