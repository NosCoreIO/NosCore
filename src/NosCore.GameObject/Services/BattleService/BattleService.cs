// __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BattleService;

public class BattleService : IBattleService
{
    public async Task Hit(IAliveEntity origin, IAliveEntity target, HitArguments arguments)
    {
        try
        {
            if ((origin.Hp <= 0) || (target.Hp <= 0))
            {
                await Cancel(origin, target);
                return;
            }

            if (origin.NoAttack)
            {
                await Cancel(origin, target);
                // CANT_ATTACK
                return;
            }

            if (arguments is { MapX: not null, MapY: not null })
            {
                //todo check max distance
                origin.PositionX = arguments.MapX.Value;
                origin.PositionY = arguments.MapY.Value;
            }

            var skillResult = await GetSkill(origin, arguments.SkillId);

            var damage = await CalculateDamage(origin, target);

            await target.HitSemaphore.WaitAsync();

            var targetIsAlive = true;
            var newHp = target.Hp - damage.Damage;
            var uselessDamage = 0;
            if (newHp <= 0)
            {
                uselessDamage = -newHp;
                newHp = 0;
                targetIsAlive = false;
            }

            target.HitList.AddOrUpdate(origin, damage.Damage - uselessDamage, (_, oldValue) => oldValue + damage.Damage - uselessDamage);
            target.Hp = newHp;

            if (!targetIsAlive)
            {
                await HandleReward(target);
            }

            await target.MapInstance.SendPacketAsync(new SuPacket
            {
                VisualType = origin.VisualType,
                VisualId = origin.VisualId,
                TargetVisualType = target.VisualType,
                TargetId = target.VisualId,
                SkillVnum = skillResult.SkillVnum,

                SkillCooldown = skillResult.SkillCooldown,
                AttackAnimation = skillResult.AttackAnimation,
                SkillEffect = skillResult.SkillEffect,
                PositionX = origin.PositionX,
                PositionY = origin.PositionY,
                TargetIsAlive = targetIsAlive,
                HpPercentage = (byte)((target.Hp / (float)target.MaxHp) * 100),
                Damage = (uint)damage.Damage,
                HitMode = damage.HitMode,
                SkillTypeMinusOne = skillResult.SkillTypeMinusOne,
            });

            await HandleCooldown(origin, skillResult);
        }
        catch
        {
            await Cancel(origin, target);
        }
        finally
        {
            target.HitSemaphore.Release();
        }
    }

    private Task HandleCooldown(IAliveEntity origin, SkillResult skill)
    {
        if (origin is ICharacterEntity character)
        {
            _ = Task.Run(async () =>
             {
                 await Task.Delay(skill.SkillCooldown * 100);
                 await character.SendPacketAsync(new SkillResetPacket()
                 {
                     CastId = skill.CastId
                 });
             });
        }
        return Task.CompletedTask;
    }

    private async Task HandleReward(IAliveEntity target)
    {
        var damageEntities = target.HitList.ToList();
        foreach (var damageEntity in damageEntities)
        {
            if (damageEntity.Key.IsAlive && damageEntity.Key.MapInstanceId == target.MapInstanceId)
            {
                var percentageDamage = (float)damageEntity.Value / damageEntities.Sum(x => x.Value);
                await FullReward(damageEntity.Key, target);
            }
        }

        target.HitList.Clear();
        return;

        Task FullReward(IAliveEntity received, IAliveEntity target)
        {
            switch (received.VisualType)
            {
                case VisualType.Player:
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }
    }

    private Task<SkillResult> GetSkill(IAliveEntity origin, long argumentsSkillId)
    {
        var skillVnum = 0;
        var skillCooldown = 0;
        var attackAnimation = 0;
        var skillEffect = 0;
        var skillTypeMinusOne = 0;
        if (origin is ICharacterEntity character)
        {
            var ski = character.Skills.Values.First(s =>
                s.Skill?.CastId == argumentsSkillId && s.Skill?.UpgradeSkill == 0);
            var skillinfo = character.Skills.Select(s => s.Value).OrderBy(o => o.SkillVNum)
                .FirstOrDefault(s =>
                    s.Skill!.UpgradeSkill == ski.Skill!.SkillVNum && s.Skill.Effect > 0 && s.Skill.SkillType == 2);
            skillVnum = ski.SkillVNum;
            skillCooldown = ski.Skill!.Cooldown;
            attackAnimation = ski.Skill.AttackAnimation;
            skillEffect = skillinfo?.Skill?.CastEffect ?? ski.Skill.CastEffect;
            skillTypeMinusOne = ski.Skill.Type - 1;
        }

        return Task.FromResult(new SkillResult
        {
            CastId = argumentsSkillId,
            SkillVnum = skillVnum,
            SkillCooldown = skillCooldown,
            AttackAnimation = attackAnimation,
            SkillEffect = skillEffect,
            SkillTypeMinusOne = skillTypeMinusOne,
        });

    }

    Task<DamageResult> CalculateDamage(IAliveEntity origin, IAliveEntity target)
    {
        return Task.FromResult(new DamageResult(100, SuPacketHitMode.SuccessAttack));
    }

    async Task Cancel(IAliveEntity origin, IAliveEntity target)
    {
        if (origin is ICharacterEntity character)
        {
            await character.SendPacketAsync(new CancelPacket()
            {
                Type = CancelPacketType.CancelAutoAttack,
                TargetId = target.VisualId
            });
        }
    }

}
