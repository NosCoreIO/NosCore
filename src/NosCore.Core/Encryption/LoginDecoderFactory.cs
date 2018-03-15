using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.Encryption
{
    public class LoginDecoderFactory : DecoderFactory
    {
        public override IDecoder GetDecoder()
        {
            return new LoginDecoder();
        }
    }
}
