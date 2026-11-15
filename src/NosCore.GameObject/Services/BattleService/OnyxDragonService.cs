//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.BattleService;

public interface IOnyxDragonService
{
    // Shows the Onyx Dragon whose extra blow the hit already took from the target.
    Task StrikeAsync(IAliveEntity attacker, IAliveEntity target, SkillInfo skill, int damage);
}

public sealed class OnyxDragonService(List<NpcMonsterDto> npcMonsters) : IOnyxDragonService, ISingletonService
{
    private const short OnyxDragonVNum = 2371;
    private const short Offset = 2;
    private static readonly TimeSpan StrikeDelay = TimeSpan.FromMilliseconds(350);

    public Task StrikeAsync(IAliveEntity attacker, IAliveEntity target, SkillInfo skill, int damage)
    {
        if (attacker.MapInstance is not { } map
            || npcMonsters.Find(m => m.NpcMonsterVNum == OnyxDragonVNum) is not { } onyx)
        {
            return Task.CompletedTask;
        }

        return attacker.OnMapAsync(async () =>
        {
            var x = (short)(attacker.PositionX + Offset);
            var y = (short)(attacker.PositionY + Offset);
            var dragon = map.SpawnMonster(onyx, x, y, false, false);
            await map.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.OnyxDragon,
                Argument = 1,
                EntityId = attacker.VisualId,
                Value = (uint)x,
                SecondValue = (uint)y,
            });
            await map.SendPacketAsync(dragon.GenerateIn());
            // Only drawn: dead, it is neither attacked nor driven by the monster AI.
            dragon.IsAlive = false;
            _ = StrikeLaterAsync(map, dragon, target, skill, damage);
        });
    }

    private static async Task StrikeLaterAsync(MapInstance map, MonsterComponentBundle dragon, IAliveEntity target,
        SkillInfo skill, int damage)
    {
        try
        {
            await Task.Delay(StrikeDelay);
            await map.Executor.RunAsync(async () =>
            {
                await map.SendPacketAsync(new SuPacket
                {
                    VisualType = dragon.VisualType,
                    VisualId = dragon.VisualId,
                    TargetVisualType = target.VisualType,
                    TargetId = target.VisualId,
                    SkillVnum = -1,
                    SkillCooldown = 0,
                    AttackAnimation = -1,
                    SkillEffect = skill.Effect,
                    PositionX = -1,
                    PositionY = -1,
                    TargetIsAlive = target.IsAlive,
                    HpPercentage = target.MaxHp > 0 ? (byte)(target.Hp * 100 / target.MaxHp) : (byte)0,
                    Damage = (uint)damage,
                    HitMode = SuPacketHitMode.SuccessAttack,
                    SkillTypeMinusOne = 0,
                    TargetCurrentHp = target.Hp,
                    TargetMaxHp = target.MaxHp,
                });
                map.RemoveMonster(dragon);
                await map.SendPacketAsync(dragon.GenerateOut());
            });
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
