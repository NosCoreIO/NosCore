//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.TransformationService;

namespace NosCore.GameObject.Messaging.ScheduledJobs
{
    [UsedImplicitly]
    public sealed class DrainSpecialistPointsHandler(ISessionRegistry sessionRegistry,
        ISpecialistTransformationService specialistTransformationService)
    {
        [UsedImplicitly]
        public async Task Handle(DrainSpecialistPointsMessage _)
        {
            foreach (var session in sessionRegistry.GetSessions())
            {
                using (await session.AcquirePacketLockAsync())
                {
                    if (session.HasPlayerEntity)
                    {
                        await specialistTransformationService.DrainPointsAsync(session);
                    }
                }
            }
        }
    }
}
