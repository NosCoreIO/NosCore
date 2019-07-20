using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data;
using NosCore.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Tests
{
    [TestClass]
    public class SetupAssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext _)
        {
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IStaticDto>()
                .IgnoreMember((member, side) => typeof(I18NString).IsAssignableFrom(member.Type));
        }
    }
}
