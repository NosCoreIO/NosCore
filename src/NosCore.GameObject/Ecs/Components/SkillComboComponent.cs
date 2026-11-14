//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Ecs.Components;

// How many times in a row the player has used the same targeted skill, for its combo animations.
public record struct SkillComboComponent(short ComboSkillVnum, short ComboHit);
