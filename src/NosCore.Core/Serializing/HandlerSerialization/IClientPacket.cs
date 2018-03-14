using NosCore.Core.Networking;

namespace NosCore.Core.Serializing
{
    public interface IClientPacket
    {
        NetworkClient Session { get; set; }

        void Handle();
    }
}