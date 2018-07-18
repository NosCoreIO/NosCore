namespace NosCore.Core.Encryption
{
    public abstract class EncoderFactory
    {
        public abstract IEncoder GetEncoder();
    }
}