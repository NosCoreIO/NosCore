using System.Collections.Generic;
using ChickenAPI.Packets.Enumerations;
using Microsoft.AspNetCore.JsonPatch;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.BazaarHttpClient
{
    public interface IBazaarHttpClient
    {
        List<BazaarLink> GetBazaarLinks(int i, int packetIndex, int pagesize, BazaarListType packetTypeFilter, byte packetSubTypeFilter, byte packetLevelFilter,
            byte packetRareFilter, byte packetUpgradeFilter, long? sellerFilter);
        LanguageKey AddBazaar(BazaarRequest bazaarRequest);
        BazaarLink GetBazaarLink(long bazaarId);
        bool Remove(long bazaarId, int count, string requestCharacterName);
        BazaarLink Modify(long bazaarId, JsonPatchDocument<BazaarLink> patchBz);
    }
}
