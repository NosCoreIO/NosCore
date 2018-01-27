using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNosCore.Database
{

    public interface IMappingBaseDAO
    {
        #region Methods

        void InitializeMapper();
        
        IMappingBaseDAO RegisterMapping(Type gameObjectType);
        
        #endregion
    }

}
