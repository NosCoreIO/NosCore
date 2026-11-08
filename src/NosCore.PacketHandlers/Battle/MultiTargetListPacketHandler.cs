//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.PathFinder.Interfaces;

namespace NosCore.PacketHandlers.Battle
{
    // A skill cast on monsters the client picked. Each listed monster must be within the skill's
    // range of the caster; the rest are dropped, and the cast costs its cooldown and mana once.
    public class MultiTargetListPacketHandler(
        IBattleService battleService,
        ISkillCastService skillCastService,
        IHeuristic distanceCalculator)
        : PacketHandler<MultiTargetListPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MultiTargetListPacket packet, ClientSession clientSession)
        {
            var character = clientSession.Character;
            if (packet.Targets == null || packet.TargetsAmount == 0 || packet.TargetsAmount != packet.Targets.Count)
            {
                return;
            }

            var map = character.MapInstance;
            if (!character.CanFight || character.IsVehicled || map == null)
            {
                await clientSession.SendPacketAsync(new CancelPacket { Type = CancelPacketType.CancelAutoAttack });
                return;
            }

            if (character.IsSitting)
            {
                await character.RestAsync();
            }

            foreach (var cast in packet.Targets.GroupBy(t => t.SkillCastId))
            {
                var skill = skillCastService.FindLearned(character, cast.Key);
                if (skill?.Skill == null)
                {
                    continue;
                }

                var targets = new List<IAliveEntity>();
                foreach (var listed in cast)
                {
                    var monster = map.GetMonsterById(listed.TargetId);
                    if (monster is not { } target)
                    {
                        continue;
                    }

                    var distance = (int)distanceCalculator.GetDistance((character.PositionX, character.PositionY), (target.PositionX, target.PositionY));
                    if (SkillReach.IsTargetInReach(skill.Skill.Range, distance, target.NpcMonster?.BasicArea ?? 0))
                    {
                        targets.Add(target);
                    }
                }

                if (targets.Count == 0)
                {
                    continue;
                }

                switch (skillCastService.Check(character, skill, out var mpCost))
                {
                    case SkillCastRefusal.NotEnoughMp:
                        await UseAoeSkillPacketHandler.NotEnoughMpAsync(character);
                        continue;
                    case SkillCastRefusal.OnCooldown:
                        continue;
                }

                if (await battleService.HitTargets(character, targets, new HitArguments { SkillId = cast.Key }))
                {
                    skillCastService.Spend(character, skill, mpCost);
                }
            }
        }
    }
}
