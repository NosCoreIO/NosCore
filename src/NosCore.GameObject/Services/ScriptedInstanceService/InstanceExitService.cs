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
        private const short GiveUpDignity = 20;

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
                await run.RunHostAsync(() =>
                {
                    host.LoseLife(run);
                    return Task.CompletedTask;
                });
                await LoseDignityAsync(session);
            }

            await run.Host.ReturnAsync(session);
        }

        // Giving up a time-space that has already started costs dignity, down to the lowest band's floor.
        private async Task LoseDignityAsync(ClientSession session)
        {
            var character = session.Character;
            var (_, floor) = dignityService.GetDignity(DignityType.Failed);
            var lost = (short)Math.Min(GiveUpDignity, Math.Max(0, character.Dignity - floor));
            if (lost == 0)
            {
                return;
            }

            character.Dignity -= lost;
            await session.SendPacketAsync(character.GenerateFd());
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Red,
                Message = Game18NConstString.DignityReduced,
                ArgumentType = 4,
                Game18NArguments = { lost }
            });
        }
    }
}
