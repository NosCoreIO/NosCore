using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.BlacklistHttpClient
{
    public class BlacklistHttpClient : IBlacklistHttpClient
    {
        public List<CharacterRelation> GetBlackLists(long characterVisualId)
        {
            throw new NotImplementedException();
        }

        public List<CharacterRelationStatus> GetBlackListsStatus(long characterVisualId)
        {
            throw new NotImplementedException();
        }

        public LanguageKey AddToBlacklist(BlacklistRequest blacklistRequest)
        {
            throw new NotImplementedException();
        }

        public void DeleteFromBlacklist(Guid characterRelationId)
        {
            throw new NotImplementedException();
        }
    }
}
