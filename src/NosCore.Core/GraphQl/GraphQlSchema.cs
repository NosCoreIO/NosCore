using GraphQL;
using GraphQL.Types;

namespace NosCore.Core.GraphQl
{
    public class GraphQlSchema : Schema
    {
        public GraphQlSchema(IDependencyResolver resolver)
            : base(resolver)
        {
            Query = resolver.Resolve<GraphQlQuery>();
            Mutation = resolver.Resolve<GraphQlMutation>();
        }
    }
}
