//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Data.Dto;
using NosCore.Packets.Interfaces;

namespace NosCore.Tests.Shared
{
    public static class TestMapping
    {
        private static readonly object Lock = new();
        private static bool _configured;

        public static void EnsureConfigured()
        {
            lock (Lock)
            {
                if (_configured)
                {
                    return;
                }

                TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;
                TypeAdapterConfig.GlobalSettings.ForDestinationType<IPacket>().Ignore(s => s.ValidationResult);
                TypeAdapterConfig.GlobalSettings.ForDestinationType<IStaticDto>()
                    .IgnoreMember((member, side) => typeof(I18NString).IsAssignableFrom(member.Type));
                _configured = true;
            }
        }
    }
}
