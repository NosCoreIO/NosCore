//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Ecs.Components;

// The last place the character stood on a base map, where leaving an instance or a miniland
// puts them back.
public record struct BaseMapPositionComponent(short MapId, short MapX, short MapY);
