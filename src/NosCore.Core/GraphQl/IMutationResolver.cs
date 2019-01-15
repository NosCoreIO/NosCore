using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.GraphQl
{
    public interface IMutationResolver
    {
        void Resolve(GraphQlMutation graphQlMutation);
    }
}
