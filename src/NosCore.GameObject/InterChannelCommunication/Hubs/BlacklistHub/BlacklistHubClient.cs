//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub
{
    public class BlacklistHubClient(IMessageBus bus) : IBlacklistHub
    {
        public Task<LanguageKey> AddBlacklistAsync(BlacklistRequest blacklistRequest) =>
            bus.RequestAsync<LanguageKey>(new AddBlacklistRequest(blacklistRequest));

        public Task<List<CharacterRelationStatus>> GetBlacklistedAsync(long id) =>
            bus.RequestAsync<List<CharacterRelationStatus>>(new GetBlacklistedRequest(id));

        public Task<bool> DeleteAsync(Guid id) => bus.RequestAsync<bool>(new DeleteBlacklistRequest(id));
    }
}
