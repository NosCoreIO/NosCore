//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NosCore.Core.Encryption
{
    public static class EncryptionExtension
    {
        public static string ToSha512(this string inputString)
        {
            using var hash = SHA512.Create();
            return string.Concat(hash.ComputeHash(Encoding.Default.GetBytes(inputString))
.Select(item => item.ToString("x2", CultureInfo.CurrentCulture)));
        }

        public static string ToPbkdf2Hash(this string inputString, string salt)
        {
            var saltBytes = Convert.FromBase64String(Convert.ToBase64String(Encoding.Default.GetBytes(salt)));

            using var pbkdf2 = new Rfc2898DeriveBytes(
                Encoding.Default.GetBytes(inputString),
                saltBytes,
                150000,
                HashAlgorithmName.SHA512);
            return string.Concat(pbkdf2.GetBytes(64).Select(item => item.ToString("x2", CultureInfo.CurrentCulture)));
        }

        public static string ToBcrypt(this string inputString, string salt)
        {
            return BCrypt.Net.BCrypt.HashPassword(inputString, salt);
        }
    }
}