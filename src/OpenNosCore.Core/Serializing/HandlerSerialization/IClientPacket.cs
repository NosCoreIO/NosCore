using OpenNosCore.Core.Networking;

namespace OpenNosCore.Core.Serializing
{
    public interface IClientPacket
    {
        NetworkClient Session { get; set; }

        void Handle();
    }
}