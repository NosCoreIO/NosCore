using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using NosCore.Core.Logger;

namespace NosCore.Database
{
    public class ItemInstanceDAO<TEntity, TDTO> : GenericDAO<TEntity,TDTO> where TEntity : class
    {
        
        public override void InitializeMapper()
        {
            // avoid override of mapping
        }

        public void InitializeMapper(Type baseType)
        {
          /*  MapperConfiguration config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap(baseType, typeof(ItemInstance))
                    .ForMember("Item", opts => opts.Ignore());

                cfg.CreateMap(typeof(ItemInstance), typeof(ItemInstanceDTO)).As(baseType);

                Type itemInstanceType = typeof(ItemInstance);
                foreach (KeyValuePair<Type, Type> entry in Mappings)
                {
                    // GameObject -> Entity
                    cfg.CreateMap(entry.Key, entry.Value).ForMember("Item", opts => opts.Ignore())
                                    .IncludeBase(baseType, typeof(ItemInstance));

                    // Entity -> GameObject
                    cfg.CreateMap(entry.Value, entry.Key)
                                    .IncludeBase(typeof(ItemInstance), baseType);

                    // Entity -> GameObject
                    cfg.CreateMap(entry.Value, typeof(ItemInstanceDTO)).As(entry.Key);
                }
            });

            Mapper = config.CreateMapper();
            */
        }



        public override IMappingBaseDAO RegisterMapping(Type gameObjectType)
        {
            try
            {
                Type targetType = Assembly.GetExecutingAssembly().GetTypes().SingleOrDefault(t => t.Name.Equals(gameObjectType.Name));
                Mappings.Add(gameObjectType, targetType);

                foreach (PropertyInfo pi in gameObjectType.GetProperties())
                {
                    object[] attrs = pi.GetCustomAttributes(typeof(KeyAttribute), false);
                    if (attrs.Length != 1)
                    {
                        continue;
                    }
                    PrimaryKey = pi;
                    break;
                }
                if (PrimaryKey != null)
                {
                    return this;
                }
                throw new KeyNotFoundException();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }

        }

    }
}
