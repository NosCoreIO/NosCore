using HotChocolate.Types;
using NosCore.Data.Enumerations;

namespace NosCore.Core
{
    public class ChannelInfoType : ObjectType<ChannelInfo>
    {
        protected override void Configure(IObjectTypeDescriptor<ChannelInfo> descriptor)
        {
            descriptor.Name("ChannelInfo");

            descriptor.Field(t => t.Id)
                .Type<NonNullType<IntType>>();

            descriptor.Field(t => t.Name)
                .Type<StringType>();

            descriptor.Field(t => t.Host)
                .Type<StringType>();

            descriptor.Field(t => t.Port)
                .Type<IntType>();

            descriptor.Field(t => t.ConnectedAccountLimit)
                .Type<IntType>();

            descriptor.Field(t => t.LastPing)
                .Type<DateTimeType>();

            descriptor.Field(t => t.Type)
                .Type<EnumType<ServerType>>();

        }
    }
}