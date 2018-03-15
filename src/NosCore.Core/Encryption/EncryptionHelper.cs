using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NosCore.Core.Encryption
{
    public class EncryptionHelper
    {
        public static string Sha512(string inputString)
        {
            using (SHA512 hash = SHA512.Create())
            {
                return string.Join(string.Empty, hash.ComputeHash(Encoding.UTF8.GetBytes(inputString)).Select(item => item.ToString("x2")));
            }
        }
    }
}
