using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;

namespace NosCore.Core.GraphQl
{
    public class GraphQlQuery : ObjectGraphType<object>
    {
        public GraphQlQuery(IEnumerable<IQueryResolver> resolversTypes)
        {
            Name = "Query";

            foreach (var resolverType in resolversTypes)
            {
                resolverType.Resolve(this);
            }
        }
    }
}

