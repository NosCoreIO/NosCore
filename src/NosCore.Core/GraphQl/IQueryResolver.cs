using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.GraphQl
{
    public interface IQueryResolver
    {
        void Resolve(GraphQlQuery graphQlQuery);
    }
}
