//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MateService;
using MateModel = NosCore.GameObject.Services.MateService.Mate;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class MateLoyaltyHandler(ISessionRegistry sessionRegistry, IMateLoyaltyService mateLoyaltyService)
    {
        [UsedImplicitly]
        public Task Handle(EntityDamagedEvent evt) =>
            !evt.Killed && evt.Damage > 0 && Owner(evt.Target) is { } owner
                ? mateLoyaltyService.HitAsync(owner.Owner, owner.Mate)
                : Task.CompletedTask;

        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt) =>
            Owner(evt.Victim) is { } owner
                ? mateLoyaltyService.KnockedOutAsync(owner.Owner, owner.Mate)
                : Task.CompletedTask;

        private (PlayerComponentBundle Owner, MateModel Mate)? Owner(IAliveEntity entity) =>
            entity is MateComponentBundle mate && sessionRegistry.TryGetCharacter(s => s.CharacterId == mate.OwnerId, out var owner)
                ? (owner, mate.Mate)
                : null;
    }
}
