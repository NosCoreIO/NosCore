using HotChocolate.Types;
using NosCore.Data.WebApi;

namespace NosCore.Data.GraphQL
{
    public class CharacterType : ObjectType<Character>
    {
        protected override void Configure(IObjectTypeDescriptor<Character> descriptor)
        {
            descriptor.Name("Character");

            descriptor.Field(t => t.Id)
                .Type<NonNullType<LongType>>();

            descriptor.Field(t => t.Name)
                .Type<StringType>();
        }
    }
}