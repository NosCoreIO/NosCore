using System.Collections.Generic;
using GraphQL.Types;

namespace NosCore.Core.GraphQl
{
    public class GraphQlMutation : ObjectGraphType<object>
    {
        public GraphQlMutation(IEnumerable<IMutationResolver> resolversTypes)
        {
            Name = "Mutation";

            foreach (var resolverType in resolversTypes)
            {
                resolverType.Resolve(this);
            }
        }
    }
}
