//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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

        IEnumerable<WpPacket> GenerateWp(short mapId);

        Task<ScriptedInstanceRun?> InstantiateAsync(ScriptedInstance entrance);

        ScriptedInstanceRun? GetRun(Guid mapInstanceId);

        Task<bool> DisposeIfEmptyAsync(ScriptedInstanceRun run);
    }
}
