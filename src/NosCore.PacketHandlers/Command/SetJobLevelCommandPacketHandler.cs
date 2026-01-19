//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Command
{
    public class SetJobLevelCommandPacketHandler(IPubSubHub pubSubHub)
        : PacketHandler<SetJobLevelCommandPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetJobLevelCommandPacket levelPacket, ClientSession session)
        {
            if (string.IsNullOrEmpty(levelPacket.Name) || (levelPacket.Name == session.Character.Name))
            {
                await session.Character.SetJobLevelAsync(levelPacket.Level);
                return;
            }

            var data = new StatData
            {
                ActionType = UpdateStatActionType.UpdateJobLevel,
                Character = new Character { Name = levelPacket.Name },
                Data = levelPacket.Level
            };

            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Name == levelPacket.Name);

            if (receiver == null)
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
                return;
            }

            await pubSubHub.SendMessageAsync(data);
        }
    }
}
