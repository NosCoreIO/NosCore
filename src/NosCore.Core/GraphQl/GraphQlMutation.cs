using GraphQL.Types;

namespace NosCore.Core.GraphQl
{
    public class GraphQlMutation : ObjectGraphType<object>
    {
        public GraphQlMutation()
        {
            Name = "Mutation";
        }
    }
}
