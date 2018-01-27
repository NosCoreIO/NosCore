namespace OpenNosCore.Core.Encryption
{
    public interface IEncryptor
    {
        bool HasCustomParameter { get; }
        string Sha512(string inputString);

        string Decrypt(byte[] data, int sessionId = 0);

        string DecryptCustomParameter(byte[] data);

        byte[] Encrypt(string data);
    }
}