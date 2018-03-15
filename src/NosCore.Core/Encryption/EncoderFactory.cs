using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.Encryption
{
    public abstract class EncoderFactory
    {
        public abstract IEncoder GetEncoder();
    }

}
