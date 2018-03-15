using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.Encryption
{
    public class WorldEncoderFactory : EncoderFactory
    {
        public override IEncoder GetEncoder()
        {
            return new WorldEncoder();
        }
    }
}
