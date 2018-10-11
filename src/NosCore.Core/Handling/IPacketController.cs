using NosCore.Core.Networking;

namespace NosCore.Core.Handling
{
    public interface IPacketController
    {
        void RegisterSession(NetworkClient clientSession);
    }
}