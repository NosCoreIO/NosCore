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
            TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;

            /*GO to Dto*/
            TypeAdapterConfig<ItemInstance, WearableInstanceDto>.NewConfig()
                .Include<ItemInstance, SpecialistInstanceDto>()
                .Include<ItemInstance, BoxInstanceDto>()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstance, SpecialistInstanceDto>.NewConfig()
                .Include<WearableInstance, BoxInstanceDto>()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstance, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstance, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance, SpecialistInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance, WearableInstanceDto>.NewConfig()
                .MapWith(src => null);

            ///*Dto to GO*/
            TypeAdapterConfig<ItemInstanceDto, WearableInstance>.NewConfig()
                .Include<ItemInstanceDto, SpecialistInstance>()
                .Include<ItemInstanceDto, BoxInstance>()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstanceDto, SpecialistInstance>.NewConfig()
                .Include<WearableInstanceDto, BoxInstance>()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstanceDto, BoxInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstanceDto, BoxInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto, SpecialistInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto, WearableInstance>.NewConfig()
                .MapWith(src => null);

            /*GO to GO*/
            TypeAdapterConfig<ItemInstance, WearableInstance>.NewConfig()
                .Include<ItemInstance, SpecialistInstance>()
                .Include<ItemInstance, BoxInstance>()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstance, SpecialistInstance>.NewConfig()
                .Include<WearableInstance, BoxInstance>()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstance, BoxInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstance, BoxInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance, SpecialistInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance, WearableInstance>.NewConfig()
                .MapWith(src => null);

            ///*DTO to DTO*/
            TypeAdapterConfig<ItemInstanceDto, WearableInstanceDto>.NewConfig()
                .Include<ItemInstanceDto, SpecialistInstanceDto>()
                .Include<ItemInstanceDto, BoxInstanceDto>()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstanceDto, SpecialistInstanceDto>.NewConfig()
                .Include<WearableInstanceDto, BoxInstanceDto>()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstanceDto, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstanceDto, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto, SpecialistInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto, WearableInstanceDto>.ForType()
                .MapWith(src => null);
        }
    }
}
