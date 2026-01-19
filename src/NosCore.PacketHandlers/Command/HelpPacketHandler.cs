//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Extensions;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class HelpPacketHandler : PacketHandler<HelpPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(HelpPacket helpPacket, ClientSession session)
        {
            await session.SendPacketAsync(session.Character.GenerateSay("-------------Help command-------------",
                SayColorType.Red));
            var classes = helpPacket.GetType().Assembly.GetTypes().Where(t =>
                    typeof(ICommandPacket).IsAssignableFrom(t)
                    && (t.GetCustomAttribute<CommandPacketHeaderAttribute>()?.Authority <= session.Account.Authority))
                .OrderBy(x => x.Name).ToList();
            foreach (var type in classes)
            {
                var classInstance = type.CreateInstance<ICommandPacket>();
                var method = type.GetMethod("Help");
                if (method == null)
                {
                    continue;
                }

                var message = method.Invoke(classInstance, null)?.ToString();
                if (!string.IsNullOrEmpty(message))
                {
                    await session.SendPacketAsync(session.Character.GenerateSay(message, SayColorType.Green));
                }
            }
        }
    }
}
