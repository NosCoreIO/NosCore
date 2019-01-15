using GraphQL.Types;
using NosCore.Data.AliveEntities;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.GraphQl
{
    public class ConnectedAccount
    {
        public string Name { get; set; }
        public int ChannelId { get; set; }
        public RegionType Language { get; set; }
        public ConnectedCharacter ConnectedCharacter { get; set; }
    }

    public class ConnectedCharacter
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class ConnectedAccountType : ObjectGraphType<ConnectedAccount>, IGraphQlType
    {
        public ConnectedAccountType()
        {
            Field(x => x.Name).Description("The connected account name.");
            Field(x => x.Language, type: typeof(LanguageType)).Description("The language of the account.");
            Field(x => x.ChannelId).Description("The channelId of the connected account.");
            Field(x => x.ConnectedCharacter, type: typeof(CharacterType)).Description("The connected character of the account.");
        }
    }

    public class CharacterType : ObjectGraphType<ConnectedCharacter>, IGraphQlType
    {
        public CharacterType()
        {
            Field(x => x.Id).Description("The Id of the character");
            Field(x => x.Name).Description("The name of the character");
        }
    }

    public class LanguageType : EnumerationGraphType<RegionType>, IGraphQlType
    {
        public LanguageType()
        {
        }
    }
}
