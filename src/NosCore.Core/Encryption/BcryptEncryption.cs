//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Authentication;
using System;

namespace NosCore.Core.Encryption
{
    public class BcryptHasher : IHasher
    {
        public string Hash(string password, string? salt)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, salt);
        }

        public string Hash(string password) => throw new NotImplementedException();
    }
}
