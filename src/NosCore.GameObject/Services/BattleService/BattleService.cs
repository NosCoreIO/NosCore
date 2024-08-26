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
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;

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

            var skillResult = await GetSkill(origin.VisualType, arguments.SkillId);

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

    private async Task HandleReward(IAliveEntity target)
    {
        var damageEntities = target.HitList.ToList();
        foreach (var damageEntity in damageEntities)
        {
            if (damageEntity.Key.IsAlive && damageEntity.Key.MapInstanceId == target.MapInstanceId)
            {
                var percentageDamage = (float)damageEntity.Value / damageEntities.Sum(x=>x.Value);
                if (percentageDamage > 0.2)
                {
                    await FullReward(damageEntity.Key, target);
                }
                else if (percentageDamage > 0.2)
                {
                    await HalfReward(damageEntity.Key, target);
                }
                else
                {
                    await NoReward(damageEntity.Key, target);
                }
            }
        }

        target.HitList.Clear();
        return;

        Task FullReward(IAliveEntity received, IAliveEntity target)
        {
            switch (received.VisualType)
            {
                case VisualType.Player:
                    var character = received as ICharacterEntity;
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }

        Task HalfReward(IAliveEntity received, IAliveEntity target)
        {
            return Task.CompletedTask;
        }

        Task NoReward(IAliveEntity received, IAliveEntity target)
        {
            return Task.CompletedTask;
        }
    }

    private Task<SkillResult> GetSkill(VisualType originVisualType, long argumentsSkillId)
    {
        return Task.FromResult(new SkillResult
        {
            SkillVnum = 240,
            SkillCooldown = 7,
            AttackAnimation = 11,
            SkillEffect = 257,
            SkillTypeMinusOne = 175,
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