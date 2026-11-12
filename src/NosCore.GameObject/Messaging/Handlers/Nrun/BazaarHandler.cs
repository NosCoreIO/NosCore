//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class BazaarHandler(IClock clock, INpcDialogService npcDialogService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.OpenNosBazaar;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle npc)
            {
                return Task.CompletedTask;
            }

            npcDialogService.Open(session.Character, npc, NpcDialogWindow.Bazaar);

            return session.SendPacketAsync(session.Character.GenerateBazaarWindow(clock));
        }
    }
}
