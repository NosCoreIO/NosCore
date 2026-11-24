//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public interface ITimeSpaceEntryService
    {
        // While a party member is in a run of this time-space, asks whether to join them instead,
        // unless the player has just said no.
        Task ShowAsync(ClientSession session, ScriptedInstance entrance, bool offerToJoin = true);

        // Asks the gold the stone's record button costs; the answer enters it as a record challenge.
        Task AskRecordChallengeAsync(ClientSession session, ScriptedInstance entrance);

        Task EnterAsync(ClientSession session, ScriptedInstance entrance, bool loserMode = false,
            bool recordChallenge = false);

        Task JoinAsync(ClientSession session, ScriptedInstance entrance, long memberId);

        Task EnterWithStoneAsync(ClientSession session, InventoryItemInstance stone);
    }
}
