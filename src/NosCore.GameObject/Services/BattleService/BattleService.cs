// __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Linq;
using System.Threading.Tasks;

using Arch.Core;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

public class BattleService(ISessionRegistry sessionRegistry) : IBattleService
{
    public async Task Hit(PlayerContext player, Entity target, HitArguments arguments)
    {
        var world = player.World;
        var targetMapInstance = player.MapInstance;
        var targetCombatState = target.GetCombatState(world);
        if (targetCombatState == null)
        {
            return;
        }

        try
        {
            if ((player.Hp <= 0) || (target.GetHp(world) <= 0))
            {
                await Cancel(player, target, world);
                return;
            }

            if (player.NoAttack)
            {
                await Cancel(player, target, world);
                return;
            }

            if (arguments is { MapX: not null, MapY: not null })
            {
                player.SetPosition(arguments.MapX.Value, arguments.MapY.Value);
            }

            var skillResult = GetSkill(player, arguments.SkillId);

            var damage = CalculateDamage(player, target, world);

            await targetCombatState.HitSemaphore.WaitAsync();

            var targetIsAlive = true;
            var newHp = target.GetHp(world) - damage.Damage;
            var uselessDamage = 0;
            if (newHp <= 0)
            {
                uselessDamage = -newHp;
                newHp = 0;
                targetIsAlive = false;
            }

            targetCombatState.HitList.AddOrUpdate(player.Entity, damage.Damage - uselessDamage, (_, oldValue) => oldValue + damage.Damage - uselessDamage);
            target.SetHp(world, newHp);

            if (!targetIsAlive)
            {
                await HandleReward(target, targetMapInstance, targetCombatState);
            }

            await targetMapInstance.SendPacketAsync(new SuPacket
            {
                VisualType = player.VisualType,
                VisualId = player.VisualId,
                TargetVisualType = target.GetVisualType(world),
                TargetId = target.GetVisualId(world),
                SkillVnum = skillResult.SkillVnum,
                SkillCooldown = skillResult.SkillCooldown,
                AttackAnimation = skillResult.AttackAnimation,
                SkillEffect = skillResult.SkillEffect,
                PositionX = player.PositionX,
                PositionY = player.PositionY,
                TargetIsAlive = targetIsAlive,
                HpPercentage = (byte)((target.GetHp(world) / (float)target.GetMaxHp(world)) * 100),
                Damage = (uint)damage.Damage,
                HitMode = damage.HitMode,
                SkillTypeMinusOne = skillResult.SkillTypeMinusOne,
            });

            _ = HandleCooldown(player, skillResult);
        }
        catch
        {
            await Cancel(player, target, world);
        }
        finally
        {
            targetCombatState.HitSemaphore.Release();
        }
    }

    private async Task HandleCooldown(PlayerContext player, SkillResult skill)
    {
        await Task.Delay(skill.SkillCooldown * 100);
        var sender = sessionRegistry.GetSenderByCharacterId(player.CharacterId);
        await (sender?.SendPacketAsync(new SkillResetPacket()
        {
            CastId = skill.CastId
        }) ?? Task.CompletedTask);
    }

    private async Task HandleReward(Entity target, MapInstance targetMapInstance, CombatState targetCombatState)
    {
        var world = targetMapInstance.EcsWorld;
        var damageEntities = targetCombatState.HitList.ToList();
        foreach (var damageEntity in damageEntities)
        {
            if (damageEntity.Key.GetIsAlive(world))
            {
                var percentageDamage = (float)damageEntity.Value / damageEntities.Sum(x => x.Value);
                await FullReward(damageEntity.Key, target, world);
            }
        }

        targetCombatState.HitList.Clear();
        return;

        Task FullReward(Entity received, Entity target, MapWorld world)
        {
            switch (received.GetVisualType(world))
            {
                case VisualType.Player:
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }
    }

    private SkillResult GetSkill(PlayerContext player, long argumentsSkillId)
    {
        var skillVnum = 0;
        var skillCooldown = 0;
        var attackAnimation = 0;
        var skillEffect = 0;
        var skillTypeMinusOne = 0;

        var ski = player.Skills.Values.First(s =>
            s.Skill?.CastId == argumentsSkillId && s.Skill?.UpgradeSkill == 0);
        var skillinfo = player.Skills.Select(s => s.Value).OrderBy(o => o.SkillVNum)
            .FirstOrDefault(s =>
                s.Skill!.UpgradeSkill == ski.Skill!.SkillVNum && s.Skill.Effect > 0 && s.Skill.SkillType == 2);
        skillVnum = ski.SkillVNum;
        skillCooldown = ski.Skill!.Cooldown;
        attackAnimation = ski.Skill.AttackAnimation;
        skillEffect = skillinfo?.Skill?.CastEffect ?? ski.Skill.CastEffect;
        skillTypeMinusOne = ski.Skill.Type - 1;

        return new SkillResult
        {
            CastId = argumentsSkillId,
            SkillVnum = skillVnum,
            SkillCooldown = skillCooldown,
            AttackAnimation = attackAnimation,
            SkillEffect = skillEffect,
            SkillTypeMinusOne = skillTypeMinusOne,
        };
    }

    DamageResult CalculateDamage(PlayerContext player, Entity target, MapWorld world)
    {
        return new DamageResult(100, SuPacketHitMode.SuccessAttack);
    }

    async Task Cancel(PlayerContext player, Entity target, MapWorld world)
    {
        var sender = sessionRegistry.GetSenderByCharacterId(player.CharacterId);
        await (sender?.SendPacketAsync(new CancelPacket()
        {
            Type = CancelPacketType.CancelAutoAttack,
            TargetId = target.GetVisualId(world)
        }) ?? Task.CompletedTask);
    }
}
