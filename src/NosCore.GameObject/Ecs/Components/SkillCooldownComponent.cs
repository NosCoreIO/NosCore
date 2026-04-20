//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

// Per-skill cooldown tracker for non-player entities (monsters, pets, NPCs). Characters
// keep cooldown state on their CharacterSkill instances, so they don't need this.
public record struct SkillCooldownComponent(ConcurrentDictionary<short, Instant> NextUsableAt);
