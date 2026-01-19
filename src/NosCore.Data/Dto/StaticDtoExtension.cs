//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using FastMember;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
