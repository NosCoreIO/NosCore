//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Autofac;
using Microsoft.Extensions.Options;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;

namespace NosCore.Core.Encryption
{
    public static class HasherRegistration
    {
        public static void RegisterHasher(this ContainerBuilder builder) =>
            builder.Register(c => Create(c.Resolve<IOptions<WebApiConfiguration>>().Value.HashingType)).As<IHasher>();

        public static IHasher Create(HashingType hashingType) => hashingType switch
        {
            HashingType.BCrypt => new BcryptHasher(),
            HashingType.Pbkdf2 => new Pbkdf2Hasher(),
            _ => new Sha512Hasher()
        };
    }
}
