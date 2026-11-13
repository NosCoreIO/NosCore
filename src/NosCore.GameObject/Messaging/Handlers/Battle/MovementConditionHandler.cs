//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.SpeedCalculationService;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class MovementConditionHandler(ISpeedCalculationService speedCalculationService, IBuffService buffService)
    {
        [UsedImplicitly]
        public Task Handle(MovementConditionChangedEvent evt)
        {
            switch (evt.Target)
            {
                case PlayerComponentBundle character:
                    character.Speed = speedCalculationService.CalculateSpeed(character);
                    var cond = character.GenerateCond();
                    cond.NoAttack |= buffService.HasEffect(character, BCardEffect.SpecialAttackNoAttack);
                    cond.NoMove |= buffService.HasEffect(character, BCardEffect.MoveMovementImpossible);
                    return character.SendPacketAsync(cond);
                case INonPlayableEntity entity:
                    entity.Speed = speedCalculationService.CalculateSpeed(entity);
                    return Task.CompletedTask;
                default:
                    return Task.CompletedTask;
            }
        }
    }
}
