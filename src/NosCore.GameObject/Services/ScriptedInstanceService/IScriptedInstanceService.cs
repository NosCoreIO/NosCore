//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ServerPackets.MiniMap;
using System;
using System.Collections.Generic;
using NosCore.Packets.ServerPackets.Portals;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public interface IScriptedInstanceService
    {
        IReadOnlyList<ScriptedInstance> GetByMap(short mapId);

        ScriptedInstance? GetAt(short mapId, short positionX, short positionY);

        ScriptedInstance? GetNear(short mapId, short positionX, short positionY);

        // A stone the player has cleared is marked done; one whose level minimum the player has not reached is locked.
        IEnumerable<WpPacket> GenerateWp(short mapId, byte level, IReadOnlySet<short> completedTimeSpaces);

        IEnumerable<GpPacket> GenerateRaidGates(short mapId);

        Task<bool> LoadAsync(ScriptedInstance entrance);

        Task<ScriptedInstance?> GetRaidBySealAsync(short sealVNum);

        Task<ScriptedInstanceRun?> InstantiateAsync(ScriptedInstance entrance);

        Task<IInstanceScript?> LoadScriptAsync(string script);

        Task<ScriptedInstanceRun?> InstantiateAsync(IInstanceScript logic, MapInstanceType instanceType,
            IInstanceRunHost host);

        ScriptedInstanceRun? GetRun(Guid mapInstanceId);

        // True when the player stood on the open exit of the run they are in.
        Task<bool> UseExitAsync(ClientSession session);

        // Carries the room's players when the session stands on one of its team portals.
        Task<bool> UseTeamPortalAsync(ClientSession session);

        // True when the way out the player stands on leaves a run's first room before its start was
        // confirmed, and they were asked to start it first.
        Task<bool> AskToStartAsync(ClientSession session, Portal wayOut);

        // True when the player answered yes from the run's first room, standing on a way out of it.
        Task<bool> ConfirmStartAsync(ClientSession session, Portal wayOut);

        Task<bool> DisposeIfEmptyAsync(ScriptedInstanceRun run);

        Task TickAsync();
    }
}
