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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastMember;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.Dto
{
    public static class StaticDtoExtension
    {
        public static IDictionary<PropertyInfo, Tuple<PropertyInfo, Type>> GetI18NProperties(Type staticDto)
        {
            var dic = new Dictionary<PropertyInfo, Tuple<PropertyInfo, Type>>();
            var properties = staticDto.GetProperties();
            foreach (var prop in properties.Where(p => typeof(I18NString).IsAssignableFrom(p.PropertyType)))
            {
                var key = properties.FirstOrDefault(s => s.Name == $"{prop.Name}I18NKey");
                if (key == null)
                {
                    continue;
                }
                dic.Add(prop,
                    new Tuple<PropertyInfo, Type>(key,
                        prop.GetCustomAttribute<I18NFromAttribute>()!.Type));
            }

            return dic;
        }

        public static void InjectI18N(this IStaticDto staticDto,
            IDictionary<PropertyInfo, Tuple<PropertyInfo, Type>> propertyInfos,
            IDictionary<Type, Dictionary<string, Dictionary<RegionType, II18NDto>>> langDictionary, Array regions,
            TypeAccessor accessor)
        {
            foreach (var prop in propertyInfos)
            {
                var key = accessor[staticDto, prop.Value.Item1.Name]?.ToString() ?? "NONAME";
                var dic = new I18NString();
                foreach (var region in regions)
                {
                    if (!langDictionary[prop.Value.Item2].ContainsKey(key))
                    {
                        continue;
                    }

                    if (langDictionary[prop.Value.Item2][key].ContainsKey((RegionType)region!))
                    {
                        dic[(RegionType)region] = langDictionary[prop.Value.Item2][key][(RegionType)region].Text;
                    }
                }

                accessor[staticDto, prop.Key.Name] = dic;
            }
        }
    }
}