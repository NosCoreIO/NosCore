//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Networking.ClientSession
{
    public class RequestData<T> : RequestData
    {
        public RequestData(ClientSession clientSession, T data) : base(clientSession)
        {
            Data = data;
        }

        public T Data { get; }
    }

    public class RequestData(ClientSession clientSession) : IRequestData
    {
        public ClientSession ClientSession { get; } = clientSession;
    }

    public interface IRequestData
    {
    }
}
