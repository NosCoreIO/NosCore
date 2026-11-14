//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Map;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ServerPackets.MiniMap;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public interface IScriptedInstanceService
    {
        IReadOnlyList<ScriptedInstance> GetByMap(short mapId);

        ScriptedInstance? GetAt(short mapId, short positionX, short positionY);

        ScriptedInstance? GetNear(short mapId, short positionX, short positionY);

        IEnumerable<WpPacket> GenerateWp(short mapId);

        Task<bool> LoadAsync(ScriptedInstance entrance);

        Task<ScriptedInstanceRun?> InstantiateAsync(ScriptedInstance entrance);

        Task<IInstanceScript?> LoadScriptAsync(string script);

        Task<ScriptedInstanceRun?> InstantiateAsync(IInstanceScript logic, MapInstanceType instanceType,
            IInstanceRunHost host);

        ScriptedInstanceRun? GetRun(Guid mapInstanceId);

        Task<bool> DisposeIfEmptyAsync(ScriptedInstanceRun run);

        Task TickAsync();
    }
}
