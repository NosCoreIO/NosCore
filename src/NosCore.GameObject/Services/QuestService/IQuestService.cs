//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.Packets.ClientPackets.Quest;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.QuestService
{
    public interface IQuestService
    {
        Task RunScriptAsync(Character character);
        Task RunScriptAsync(Character character, ScriptClientPacket? packet);
    }
}
