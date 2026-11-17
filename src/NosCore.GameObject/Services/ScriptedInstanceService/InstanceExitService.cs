//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NosCore.Algorithm.DignityService;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RaidService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public sealed class InstanceExitService(IScriptedInstanceService scriptedInstanceService, IRaidService raidService,
            IDignityService dignityService)
        : IInstanceExitService, ISingletonService
    {
        public async Task LeaveAsync(ClientSession session)
        {
            var run = scriptedInstanceService.GetRun(session.Character.MapInstance.MapInstanceId);
            if (run == null)
            {
                return;
            }

            if (session.Character.Group.RaidRun == run)
            {
                await raidService.LeaveAsync(session);
                return;
            }

            if (run.Host is TimeSpaceRunHost host && run.HasStarted && run.EndState == null)
            {
                var gaveUp = false;
                await run.RunHostAsync(() =>
                {
                    gaveUp = true;
                    host.LoseLife(run);
                    return Task.CompletedTask;
                });
                if (gaveUp)
                {
                    await session.Character.LoseDignityAsync(dignityService, dignityService.GetDignityLoss(session.Character.Level));
                }
            }

            await run.Host.ReturnAsync(session);
        }
    }
}
