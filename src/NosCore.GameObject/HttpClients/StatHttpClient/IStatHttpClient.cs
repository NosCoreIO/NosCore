using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.StatHttpClient
{
    public interface IStatHttpClient
    {
        void ChangeStat(StatData data, object item1);
    }
}
