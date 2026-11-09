using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ShipService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun;

[UsedImplicitly]
public sealed class BoardMortazDesertShipHandler(IShipService ships) : INrunEventHandler
{
    public NrunRunnerType Runner => NrunRunnerType.GoToShipMortazDesert;
    public bool RequiresNpc => true;

    public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
        target is NpcComponentBundle npc
            ? ships.BoardAsync(session, npc, Runner, packet.Type ?? 0)
            : Task.CompletedTask;
}
