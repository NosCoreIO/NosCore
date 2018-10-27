using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Mapster;
using NosCore.Data;
using NosCore.GameObject.Services.ItemBuilder.Item;

namespace NosCore.GameObject.Mapping
{
    public static class Mapper
    {
        public static void InitializeMapperItemInstance()
        {
            TypeAdapterConfig<ItemInstance, WearableInstance>.NewConfig()
                .Include<ItemInstance, SpecialistInstance>()
                .Include<ItemInstance, BoxInstance>()
                .MapWith(src => null, applySettings: true);
            
            TypeAdapterConfig<WearableInstance, SpecialistInstance>.NewConfig()
                .Include<WearableInstance, BoxInstance>()
                .MapWith(src => null, applySettings: true);

            TypeAdapterConfig<SpecialistInstance, BoxInstance>.NewConfig()
                .MapWith(src => null, applySettings: true);
        }
    }
}
