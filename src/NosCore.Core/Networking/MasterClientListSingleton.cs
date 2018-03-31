using NosCore.Core;
using System.Collections.Generic;

namespace NosCore.Networking
{
    public class MasterClientListSingleton
    {
        private static MasterClientListSingleton instance;

        private MasterClientListSingleton() { }

        public static MasterClientListSingleton Instance
        {
            get
            {
                return instance ?? (instance = new MasterClientListSingleton());
            }
        }

        public List<WorldServerInfo> WorldServers { get; set; }
    }
}
