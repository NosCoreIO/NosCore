using Autofac;
using NosCore.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NosCore.Core.Serializing
{
    public static class PacketControllerFactory
    {
        private static bool IsInitialized { get; set; }
        
        private static IContainer Container { get; set; }

        public static void Initialize(IContainer container)
        {
            if (!IsInitialized)
            {
                IsInitialized = true;
                Container = container;
            }
        }

        public static IEnumerable<IPacketController> GenerateControllers()
        {
           return Container.Resolve<IEnumerable<IPacketController>>();
        }
    }
}