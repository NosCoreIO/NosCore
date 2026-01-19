//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Authentication;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NosCore.Core.Encryption
{
    public class Sha512Hasher : IHasher
    {
        public string Hash(string password, string? salt)
        {
            using var hash = SHA512.Create();
            return string.Concat(hash.ComputeHash(Encoding.Default.GetBytes(salt ?? "" + password))
                .Select(item => item.ToString("x2", CultureInfo.CurrentCulture)));
        }

        public string Hash(string password) => Hash(password, null);
    }
}
