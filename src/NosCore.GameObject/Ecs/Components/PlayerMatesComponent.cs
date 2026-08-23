//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.MateService;
using System.Collections.Concurrent;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerMatesComponent(ConcurrentDictionary<long, Mate> Mates);
