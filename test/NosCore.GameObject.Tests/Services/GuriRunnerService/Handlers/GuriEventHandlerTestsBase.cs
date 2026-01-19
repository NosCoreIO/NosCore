//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.UI;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.GuriRunnerService.Handlers
{
    public abstract class GuriEventHandlerTestsBase
    {
        protected IEventHandler<GuriPacket, GuriPacket>? Handler;
        protected ClientSession? Session;
        protected readonly UseItemPacket UseItem = new();

        protected Task ExecuteGuriEventHandlerAsync(GuriPacket guriPacket)
        {
            return Handler!.ExecuteAsync(
                new RequestData<GuriPacket>(
                    Session!,
                    guriPacket));
        }
    }
}
