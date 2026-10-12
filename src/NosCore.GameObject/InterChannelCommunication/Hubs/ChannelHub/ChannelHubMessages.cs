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
