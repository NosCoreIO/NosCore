//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.MateService;
using System.Collections.Concurrent;

namespace NosCore.GameObject.Ecs.Components;

/// <summary>
/// The pets and partners a character owns, keyed by the transport id the client addresses them
/// with.
/// </summary>
/// <remarks>
/// Deliberately its own component rather than another list inside PlayerInventoryComponent: a
/// mate is not a possession that sits in a bag, it is a creature that will eventually need its
/// own position, health and turn in the fight. Putting it where the titles live would make that
/// step harder for no gain today.
/// </remarks>
public record struct PlayerMatesComponent(ConcurrentDictionary<long, Mate> Mates);
