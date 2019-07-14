using System;
using System.Collections.Generic;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.BlacklistHttpClient
{
    public interface IBlacklistHttpClient
    {
        List<CharacterRelation> GetBlackLists(long characterVisualId);
        List<CharacterRelationStatus> GetBlackListsStatus(long characterVisualId);
        LanguageKey AddToBlacklist(BlacklistRequest blacklistRequest);
        void DeleteFromBlacklist(Guid characterRelationId);
    }
}
