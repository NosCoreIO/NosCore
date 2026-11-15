// __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

// OnyxDragon: an Onyx Dragon strikes after the hit for half of its damage.
public record DamageResult(int Damage, SuPacketHitMode HitMode, bool OnyxDragon = false);
