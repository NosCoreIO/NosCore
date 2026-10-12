//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using System;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;

public record AddFriendRequest(FriendShipRequest FriendShipRequest) : IMasterRequest;

public record GetFriendsRequest(long Id) : IMasterRequest;

public record DeleteFriendRequest(Guid Id) : IMasterRequest;
