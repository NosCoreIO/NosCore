using AutoMapper;
using NosCore.DAL;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.GameObject;
using System;
using System.Linq;

namespace NosCore.Mapping
{
    static public class Mapper
    {
        public static void InitializeMapping()
        {
            MapperConfiguration config = new MapperConfiguration(cfg =>
            {
                foreach (Type type in typeof(CharacterDTO).Assembly.GetTypes().Where(t => typeof(IDTO).IsAssignableFrom(t)))
                {
                    int index = type.Name.LastIndexOf("DTO");
                    if (index >= 0)
                    {
                        string name = type.Name.Substring(0, index);
                        Type typefound = typeof(Character).Assembly.GetTypes().SingleOrDefault(t => t.Name.Equals(name));
                        Type entitytypefound = typeof(Database.Entities.Account).Assembly.GetTypes().SingleOrDefault(t => t.Name.Equals(name));
                        if (entitytypefound != null)
                        {
                            cfg.CreateMap(type, entitytypefound).ReverseMap();
                            if (typefound != null)
                            {
                                cfg.CreateMap(entitytypefound, type).As(typefound);
                            }
                        }
                    }
                }
            });
            DAOFactory.RegisterMapping(config.CreateMapper());
        }
    }
}
