using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Database
{
    public interface IDatabaseObject
    {
        #region Methods

        /// <summary>
        /// Intializes the GameObject, will be injected by AutoMapper after Entity -&gt; GO mapping
        /// </summary>
        void Initialize();

        #endregion
    }
}
