//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Authentication;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NosCore.Core.Encryption
{
    public class Pbkdf2Hasher : IHasher
    {
        public string Hash(string inputString, string? salt)
        {
            var saltBytes = Convert.FromBase64String(Convert.ToBase64String(Encoding.Default.GetBytes(salt ?? "")));

            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.Default.GetBytes(inputString),
                saltBytes,
                150000,
                HashAlgorithmName.SHA512,
                64);
            return string.Concat(hashBytes.Select(item => item.ToString("x2", CultureInfo.CurrentCulture)));
        }

        public string Hash(string password) => throw new NotImplementedException();
    }
}
