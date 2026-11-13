//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Ecs.Components;

// BankGold belongs to the account; only one of its characters is ever in the world.
public record struct GoldComponent(long Gold, long BankGold);
