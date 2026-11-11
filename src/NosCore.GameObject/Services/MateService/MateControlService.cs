//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MateService
{
    public class MateControlService(IHeuristic distanceCalculator, IBattleService battleService,
            INpcCombatCatalog catalog, List<SkillDto> skills, IClock clock) : IMateControlService
    {
        private readonly Dictionary<short, SkillDto> _skillsByVnum = skills.ToDictionary(s => s.SkillVNum);

        public async Task<bool> MoveAsync(PlayerComponentBundle owner, long mateTransportId, short x, short y)
        {
            if (!TryGetOutMate(owner, mateTransportId, out var mate, out var entity)
                || !entity.IsAlive
                || entity.NoMove
                || !owner.MapInstance.Map.IsWalkable(x, y))
            {
                return false;
            }

            // The same one-step allowance a player's own walk gets, so a client cannot teleport its mate.
            var distance = (int)distanceCalculator.GetDistance((entity.PositionX, entity.PositionY), (x, y));
            if (!SkillReach.IsWithinOneStep(distance, mate.NpcMonster.Speed))
            {
                return false;
            }

            mate.PositionX = x;
            mate.PositionY = y;
            entity.PositionX = x;
            entity.PositionY = y;

            await SendAsync(owner, new MovePacket
            {
                VisualType = VisualType.Npc,
                VisualEntityId = mate.MateTransportId,
                MapX = x,
                MapY = y,
                Speed = mate.NpcMonster.Speed
            });
            return true;
        }

        public async Task<bool> TalkAsync(PlayerComponentBundle owner, long mateTransportId, string? message)
        {
            if (string.IsNullOrWhiteSpace(message) || !TryGetOutMate(owner, mateTransportId, out _, out var entity))
            {
                return false;
            }

            await SendAsync(owner, entity.GenerateSay(message, SayColorType.Default));
            return true;
        }

        public async Task<bool> AttackAsync(PlayerComponentBundle owner, long mateTransportId, long castId,
            IAliveEntity target)
        {
            if (!TryGetOutMate(owner, mateTransportId, out var mate, out var entity)
                || target.MapInstance != owner.MapInstance)
            {
                return false;
            }

            // Only a skill in the creature's own roster, picked as the resolver picks it (cast 0 means its first
            // skill), so reach and cooldown are checked on the skill that is actually cast.
            var skill = catalog.GetSkills(mate.VNum)
                .Select(s => _skillsByVnum.GetValueOrDefault(s.SkillVNum))
                .FirstOrDefault(s => s != null && (s.CastId == castId || castId == 0));
            if (skill == null)
            {
                return false;
            }

            var now = clock.GetCurrentInstant();
            if (entity.NextUsableAt.TryGetValue(skill.SkillVNum, out var readyAt) && readyAt > now)
            {
                return false;
            }

            var distance = (int)distanceCalculator.GetDistance((entity.PositionX, entity.PositionY),
                (target.PositionX, target.PositionY));
            var targetArea = target is INonPlayableEntity { NpcMonster: { } npcMonster } ? npcMonster.BasicArea : (byte)0;
            if (!SkillReach.IsInReach(skill.TargetType, skill.Range, skill.TargetRange, distance, targetArea))
            {
                return false;
            }

            if (!await battleService.Hit(entity, target, new HitArguments { SkillId = castId }))
            {
                return false;
            }

            entity.NextUsableAt[skill.SkillVNum] = now.Plus(Duration.FromMilliseconds(Math.Max(200, skill.Cooldown * 100)));
            return true;
        }

        // Trusting the id alone would let a client drive somebody else's mate, or one left in the Miniland.
        private static bool TryGetOutMate(PlayerComponentBundle owner, long mateTransportId, [NotNullWhen(true)] out Mate? mate,
            out MateComponentBundle entity)
        {
            entity = default;
            if (!owner.Mates.TryGetValue(mateTransportId, out mate)
                || !mate.IsTeamMember
                || mate.Entity is not { } outMate)
            {
                return false;
            }

            entity = outMate;
            return true;
        }

        // Broadcasting a hidden owner's mates would put the owner back on screen.
        private static Task SendAsync(PlayerComponentBundle owner, IPacket packet) =>
            owner.Invisible ? owner.SendPacketAsync(packet) : owner.MapInstance.SendPacketAsync(packet);
    }
}
