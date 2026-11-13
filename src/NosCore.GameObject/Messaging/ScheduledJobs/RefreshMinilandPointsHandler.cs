//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MinilandService;

namespace NosCore.GameObject.Messaging.ScheduledJobs
{
    [UsedImplicitly]
    public sealed class RefreshMinilandPointsHandler(
        IClock clock,
        ISessionRegistry sessionRegistry,
        IMinilandRegistry minilandRegistry)
    {
        [UsedImplicitly]
        public async Task Handle(RefreshMinilandPointsMessage _)
        {
            var now = clock.GetCurrentInstant();
            foreach (var session in sessionRegistry.GetSessions())
            {
                using (await session.AcquirePacketLockAsync())
                {
                    if (!session.HasPlayerEntity)
                    {
                        continue;
                    }

                    minilandRegistry.GetByCharacterId(session.Character.CharacterId)?.RefreshDailyPoints(now);
                }
            }
        }
    }
}
