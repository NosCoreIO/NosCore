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