//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.BlackListService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub
{
    public class BlacklistHub(IBlacklistService blacklistService) : IBlacklistHub
    {
        public Task<LanguageKey> AddBlacklistAsync(BlacklistRequest blacklistRequest) => blacklistService.BlacklistPlayerAsync(blacklistRequest.CharacterId, blacklistRequest.BlInsPacket!.CharacterId);

        public Task<List<CharacterRelationStatus>> GetBlacklistedAsync(long id) => blacklistService.GetBlacklistedListAsync(id);

        public async Task<bool> DeleteAsync(Guid id) => await blacklistService.UnblacklistAsync(id);

        public static async Task<Reply<LanguageKey>> Handle(AddBlacklistRequest r, IServiceProvider services) => new(await services.GetRequiredService<BlacklistHub>().AddBlacklistAsync(r.BlacklistRequest));

        public static async Task<Reply<List<CharacterRelationStatus>>> Handle(GetBlacklistedRequest r, IServiceProvider services) => new(await services.GetRequiredService<BlacklistHub>().GetBlacklistedAsync(r.Id));

        public static async Task<Reply<bool>> Handle(DeleteBlacklistRequest r, IServiceProvider services) => new(await services.GetRequiredService<BlacklistHub>().DeleteAsync(r.Id));
    }
}
