using System.Collections.Generic;
using ChickenAPI.Packets.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.BazaarHttpClient
{
    public interface IBazaarHttpClient
    {
        List<BazaarLink> GetBazaarLinks(int i, int packetIndex, int pagesize, BazaarListType packetTypeFilter, byte packetSubTypeFilter, byte packetLevelFilter, byte packetRareFilter, byte packetUpgradeFilter, object o1);
        LanguageKey AddBazaar(BazaarRequest bazaarRequest);
        List<BazaarLink> GetBazaarLinks(long bazaarId);
    }
}
