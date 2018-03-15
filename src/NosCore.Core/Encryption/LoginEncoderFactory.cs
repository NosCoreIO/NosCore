using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.Encryption
{
    public class LoginEncoderFactory : EncoderFactory
    {
        public override IEncoder GetEncoder()
        {
            return new LoginEncoder();
        }
    }
}
