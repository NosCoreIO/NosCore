//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public readonly record struct WaveMonster(short VNum, short Amount, bool IsMoving);

    // What differs between the kinds of scripted instance: where their data comes from, what
    // an ending gives the players, and where the players go afterwards.
    public interface IInstanceRunHost
    {
        string Name { get; }

        IEnumerable<WaveMonster> WaveMonsters(byte wave);

        Task DropWaveAsync(MapInstance room, byte wave);

        Task EndedAsync(ScriptedInstanceRun run, EndStateType state);

        Task ReturnAsync(ClientSession session);
    }
}
