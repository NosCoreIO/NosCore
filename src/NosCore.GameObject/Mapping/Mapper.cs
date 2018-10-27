using Mapster;
using NosCore.Data;
using NosCore.GameObject.Services.ItemBuilder.Item;

namespace NosCore.GameObject.Mapping
{
    public static class Mapper
    {
        //TODO cleanup
        public static void InitializeMapperItemInstance()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;

            /*GO to Dto*/
            TypeAdapterConfig<ItemInstance, WearableInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstance, SpecialistInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstance, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstance, SpecialistInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<WearableInstance, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstance, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstance, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance, SpecialistInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance, WearableInstanceDto>.NewConfig()
                .MapWith(src => null);

            /*Dto to GO*/
            TypeAdapterConfig<ItemInstanceDto, WearableInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstanceDto, SpecialistInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstanceDto, BoxInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<ItemInstanceDto, SpecialistInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<ItemInstanceDto, BoxInstance>.NewConfig()
                .MapWith(src => null);


            TypeAdapterConfig<WearableInstanceDto, BoxInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstanceDto, SpecialistInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstanceDto, BoxInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstanceDto, BoxInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto, SpecialistInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto, WearableInstance>.NewConfig()
                .MapWith(src => null);

            ///*GO to GO*/
            TypeAdapterConfig<ItemInstance, WearableInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<ItemInstance, BoxInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<ItemInstance, SpecialistInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstance, BoxInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstance, SpecialistInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstance, BoxInstance>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstance, BoxInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance, SpecialistInstance>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance, WearableInstance>.NewConfig()
                .MapWith(src => null);

            /*DTO to DTO*/
            TypeAdapterConfig<ItemInstanceDto, WearableInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstanceDto, SpecialistInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstanceDto, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstanceDto, SpecialistInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<WearableInstanceDto, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<SpecialistInstanceDto, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<SpecialistInstanceDto, WearableInstanceDto>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstanceDto, BoxInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto, SpecialistInstanceDto>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto, WearableInstanceDto>.NewConfig()
                .MapWith(src => null);
        }
    }
}
