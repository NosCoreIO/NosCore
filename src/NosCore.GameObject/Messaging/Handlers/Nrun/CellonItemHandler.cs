//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.NpcDialogService;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class CellonItemHandler(INpcDialogService npcDialogService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.CellonItem;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle npc)
            {
                return Task.CompletedTask;
            }

            npcDialogService.Open(session.Character, npc, NpcDialogWindow.Upgrade);

            return session.SendPacketAsync(new WopenPacket
            {
                Type = WindowType.CellonItem,
            });
        }
    }
}
