//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using System;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;

public record AddBlacklistRequest(BlacklistRequest BlacklistRequest) : IMasterRequest;

public record GetBlacklistedRequest(long Id) : IMasterRequest;

public record DeleteBlacklistRequest(Guid Id) : IMasterRequest;
