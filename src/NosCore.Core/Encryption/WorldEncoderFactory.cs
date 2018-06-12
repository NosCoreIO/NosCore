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