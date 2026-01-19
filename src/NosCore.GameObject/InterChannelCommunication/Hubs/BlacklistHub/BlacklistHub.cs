//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.SignalR;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.BlackListService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub
{
    public class BlacklistHub(IBlacklistService blacklistService) : Hub, IBlacklistHub
    {
        public Task<LanguageKey> AddBlacklistAsync(BlacklistRequest blacklistRequest) => blacklistService.BlacklistPlayerAsync(blacklistRequest.CharacterId, blacklistRequest.BlInsPacket!.CharacterId);

        public Task<List<CharacterRelationStatus>> GetBlacklistedAsync(long id) => blacklistService.GetBlacklistedListAsync(id);

        public async Task<bool> DeleteAsync(Guid id) => await blacklistService.UnblacklistAsync(id);
    }
}
