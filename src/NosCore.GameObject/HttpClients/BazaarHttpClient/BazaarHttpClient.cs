using System;
using System.Collections.Generic;
using System.Text;
using ChickenAPI.Packets.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.BazaarHttpClient
{
    public class BazaarHttpClient : IBazaarHttpClient
    {
        public List<BazaarLink> GetBazaarLinks(int i, int packetIndex, int pagesize, BazaarListType packetTypeFilter, byte packetSubTypeFilter,
            byte packetLevelFilter, byte packetRareFilter, byte packetUpgradeFilter, object o1)
        {
            throw new NotImplementedException();
        }

        public LanguageKey AddBazaar(BazaarRequest bazaarRequest)
        {
            throw new NotImplementedException();
        }

        public List<BazaarLink> GetBazaarLinks(long bazaarId)
        {
            throw new NotImplementedException();
        }
    }
}
