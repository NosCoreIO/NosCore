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

using System.Globalization;
using System.Reflection;
using System.Resources;
using NosCore.Shared.Enumerations;

namespace NosCore.Shared.I18N
{
    public sealed class Language
    {
        private static Language _instance;
        private readonly ResourceManager _manager;

        private Language()
        {
            if (Assembly.GetExecutingAssembly() != null)
            {
                _manager = new ResourceManager(
                    Assembly.GetExecutingAssembly().GetName().Name + ".Resource.LocalizedResources",
                    Assembly.GetExecutingAssembly());
            }
        }

        public static Language Instance => _instance ?? (_instance = new Language());

        public string GetMessageFromKey(LanguageKey messageKey, RegionType culture)
        {
            var cult = new CultureInfo(culture.ToString());
            var resourceMessage = _manager != null && messageKey.ToString() != null
                ? _manager.GetResourceSet(cult, true,
                        cult.TwoLetterISOLanguageName == default(RegionType).ToString().ToLower())
                    ?.GetString(messageKey.ToString()) : string.Empty;

            return !string.IsNullOrEmpty(resourceMessage) ? resourceMessage : $"#<{messageKey.ToString()}>";
        }
    }
}