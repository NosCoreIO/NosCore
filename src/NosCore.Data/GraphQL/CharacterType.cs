using HotChocolate.Types;

namespace NosCore.Data.GraphQL
{
    public class CharacterType
        : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Character");

            descriptor.Field("id")
                .Type<NonNullType<IntType>>();

            descriptor.Field("name")
                .Type<StringType>();
        }
    }
}