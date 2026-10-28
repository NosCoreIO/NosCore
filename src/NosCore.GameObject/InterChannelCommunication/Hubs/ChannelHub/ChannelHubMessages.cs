//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;

public record BindChannelRequest(string ChannelKey, Channel Channel) : IMasterRequest;

public record GetCommunicationChannelsRequest : IMasterRequest;

public record SetMaintenanceRequest(string ChannelKey, bool IsGlobal, bool Value) : IMasterRequest;

public record PingChannelRequest(string ChannelKey) : IMasterRequest;

// Its own type rather than Reply<bool> so logger.yml can drop the per-second heartbeat logs alone.
public record PingChannelReply(bool IsRegistered);
