using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.Encryption
{
    public abstract class DecoderFactory
    {
        public abstract IDecoder GetDecoder();
    }

}
