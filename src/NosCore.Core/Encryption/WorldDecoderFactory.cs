using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.Encryption
{
    public class WorldDecoderFactory : DecoderFactory
    {
        public override IDecoder GetDecoder()
        {
            return new WorldDecoder();
        }
    }
}
