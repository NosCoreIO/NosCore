using FastMember;
using NosCore.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NosCore.Data.StaticEntities
{
    public static class StaticDtoExtension
    {
        public static IDictionary<PropertyInfo, Tuple<PropertyInfo, Type>> GetI18NProperties(Type staticDto)
        {
            var dic = new Dictionary<PropertyInfo, Tuple<PropertyInfo, Type>>();
            var properties = staticDto.GetProperties();
            foreach (var prop in properties.Where(p => typeof(I18NString).IsAssignableFrom(p.PropertyType)))
            {
                dic.Add(prop,
                    new Tuple<PropertyInfo, Type>(properties.FirstOrDefault(s => s.Name == $"{prop.Name}I18NKey"),
                        prop.GetCustomAttribute<I18NFromAttribute>().Type));
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
                foreach (RegionType region in regions)
                {
                    if (langDictionary[prop.Value.Item2].ContainsKey(key))
                    {
                        if (langDictionary[prop.Value.Item2][key].ContainsKey(region))
                        {
                            dic[region] = langDictionary[prop.Value.Item2][key][region].Text;
                        }
                    }
                }
                accessor[staticDto, prop.Key.Name] = dic;
            }
        }
    }
}
