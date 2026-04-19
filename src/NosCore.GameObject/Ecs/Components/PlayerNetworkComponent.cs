//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerNetworkComponent(IPacketSender? Sender, IChannel? Channel);
