//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.FriendService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub
{
    public class FriendHub(IFriendService friendService) : IFriendHub
    {
        public Task<LanguageKey> AddFriendAsync(FriendShipRequest friendPacket) => friendService.AddFriendAsync(friendPacket.CharacterId, friendPacket.FinsPacket!.CharacterId, friendPacket.FinsPacket.Type);

        public Task<List<CharacterRelationStatus>> GetFriendsAsync(long id) => friendService.GetFriendsAsync(id);

        public async Task<bool> DeleteAsync(Guid id) => await friendService.DeleteAsync(id);

        public static async Task<Reply<LanguageKey>> Handle(AddFriendRequest r, IServiceProvider services) => new(await services.GetRequiredService<FriendHub>().AddFriendAsync(r.FriendShipRequest));

        public static async Task<Reply<List<CharacterRelationStatus>>> Handle(GetFriendsRequest r, IServiceProvider services) => new(await services.GetRequiredService<FriendHub>().GetFriendsAsync(r.Id));

        public static async Task<Reply<bool>> Handle(DeleteFriendRequest r, IServiceProvider services) => new(await services.GetRequiredService<FriendHub>().DeleteAsync(r.Id));
    }
}
