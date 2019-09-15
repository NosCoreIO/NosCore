using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.HttpClients.BlacklistHttpClient
{
    public interface IBlacklistHttpClient
    {
        List<CharacterRelationStatus> GetBlackLists(long characterVisualId);
        LanguageKey AddToBlacklist(BlacklistRequest blacklistRequest);
        void DeleteFromBlacklist(Guid characterRelationId);
    }
}
