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