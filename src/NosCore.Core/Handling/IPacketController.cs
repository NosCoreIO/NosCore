using NosCore.Core.Networking;

namespace NosCore.Core
{
    public interface IPacketController
    {
        void RegisterSession(NetworkClient clientSession);
    }
}