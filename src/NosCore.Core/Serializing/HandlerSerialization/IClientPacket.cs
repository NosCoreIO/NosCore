using NosCore.Core.Networking;

namespace NosCore.Core.Serializing.HandlerSerialization
{
    public interface IClientPacket
    {
        NetworkClient Session { get; set; }

        void Handle();
    }
}