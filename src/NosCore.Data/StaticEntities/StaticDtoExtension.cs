using NosCore.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FastMember;

namespace NosCore.Data.StaticEntities
{
    public static class StaticDtoExtension
    {
        public static IDictionary<PropertyInfo, Tuple<PropertyInfo, Type>> GetI18NProperties(Type staticDto)
        {
            var dic = new Dictionary<PropertyInfo, Tuple<PropertyInfo, Type>>();
            var properties = staticDto.GetProperties();
            foreach (var prop in properties.Where(p => typeof(IDictionary<RegionType, string>).IsAssignableFrom(p.PropertyType)))
            {
                dic.Add(prop, new Tuple<PropertyInfo, Type>(properties.FirstOrDefault(s => s.Name == $"{prop.Name}I18NKey"), prop.GetCustomAttribute<I18NFromAttribute>().Type));
            }

            return dic;
        }

        public static void InjectI18N(this IStaticDto staticDto, IDictionary<PropertyInfo, Tuple<PropertyInfo, Type>> propertyInfos, IDictionary<Type, List<II18NDto>> langDictionary, Array regions, TypeAccessor accessor)
        {
            foreach (var prop in propertyInfos)
            {
                var key = accessor[staticDto, prop.Value.Item1.Name]?.ToString() ?? "NONAME";
                var list = langDictionary[prop.Value.Item2].Where(s => s.Key == key).Take(regions.Length);
                var dic = new Dictionary<RegionType, string>();
                foreach (RegionType region in regions)
                {
                    dic.Add(region, list.FirstOrDefault(s => s.Key == key && s.RegionType == region)?.Text ?? key);
                }

                accessor[staticDto, prop.Key.Name] = dic;
            }
        }
    }
}
