//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub
{
    public class BlacklistHubClient(HubConnectionFactory hubConnectionFactory, ILogger logger)
        : BaseHubClient(hubConnectionFactory, nameof(BlacklistHub), logger), IBlacklistHub
    {
        public Task<LanguageKey> AddBlacklistAsync(BlacklistRequest blacklistRequest) =>
            InvokeAsync<LanguageKey>(nameof(AddBlacklistAsync), blacklistRequest);

        public Task<List<CharacterRelationStatus>> GetBlacklistedAsync(long id) =>
            InvokeAsync<List<CharacterRelationStatus>>(nameof(GetBlacklistedAsync), id);

        public Task<bool> DeleteAsync(Guid id) =>
            InvokeAsync<bool>(nameof(DeleteAsync), id);
    }
}
